using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Uow;
using Volo.Abp.Validation;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Partial class for generic sync methods
/// </summary>
public partial class SyncAppService
{
    private IAbpDistributedLock DistributedLock => LazyServiceProvider.LazyGetRequiredService<IAbpDistributedLock>();

    private IDistributedCache<SyncCreatedCacheItem> SyncCreatedCache =>
        LazyServiceProvider.LazyGetRequiredService<IDistributedCache<SyncCreatedCacheItem>>();

    private ICurrentPrincipalAccessor CurrentPrincipalAccessor =>
        LazyServiceProvider.LazyGetRequiredService<ICurrentPrincipalAccessor>();

    private IdentityUserManager UserManager =>
        LazyServiceProvider.LazyGetRequiredService<IdentityUserManager>();

    /// <summary>
    /// OF-09: phiếu tạo/sửa offline được ghi nhận cho người dùng đã tạo nó trên máy (người tạo, người tiếp nhận...),
    /// kể cả khi lúc đồng bộ người đang đăng nhập là người khác.
    /// Trả về null (giữ người đang đăng nhập) nếu không có id, trùng người hiện tại, hoặc tài khoản không tồn tại / đã khoá.
    /// Chỉ dựng principal: CurrentPrincipalAccessor.Change() dùng AsyncLocal nên phải gọi ở hàm gọi
    /// (gọi bên trong hàm async này thì giá trị bị hoàn lại khi hàm trả về → không có tác dụng).
    /// </summary>
    private async Task<ClaimsPrincipal?> BuildOfflineUserPrincipalAsync(Guid? offlineUserId, string clientId)
    {
        if (!offlineUserId.HasValue || offlineUserId == CurrentUser.Id)
        {
            return null;
        }

        var user = await UserManager.FindByIdAsync(offlineUserId.Value.ToString());
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning(
                "Offline sync ClientId {ClientId}: offline user {OfflineUserId} not found or inactive, using current user {CurrentUserId}",
                clientId, offlineUserId, CurrentUser.Id);
            return null;
        }

        var claims = new List<Claim>
        {
            new(AbpClaimTypes.UserId, user.Id.ToString()),
            new(AbpClaimTypes.UserName, user.UserName ?? string.Empty),
            new(AbpClaimTypes.Email, user.Email ?? string.Empty),
        };
        if (!string.IsNullOrEmpty(user.Name)) claims.Add(new Claim(AbpClaimTypes.Name, user.Name));
        if (!string.IsNullOrEmpty(user.Surname)) claims.Add(new Claim(AbpClaimTypes.SurName, user.Surname));
        if (user.TenantId.HasValue) claims.Add(new Claim(AbpClaimTypes.TenantId, user.TenantId.Value.ToString()));
        foreach (var role in await UserManager.GetRolesAsync(user))
        {
            claims.Add(new Claim(AbpClaimTypes.Role, role));
        }

        _logger.LogInformation(
            "Offline sync ClientId {ClientId}: recording as offline user {OfflineUserId} ({UserName}), sent by {CurrentUserId}",
            clientId, user.Id, user.UserName, CurrentUser.Id);

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "OfflineSync"));
    }

    /// <inheritdoc />
    public async Task<GenericSyncBatchResponseDto> SyncBatchAsync(GenericSyncBatchRequestDto input)
    {
        var response = new GenericSyncBatchResponseDto
        {
            Results = new List<GenericSyncResultDto>(),
            ServerTimestamp = DateTime.UtcNow,
            SyncedByType = new Dictionary<SyncEntityType, int>()
        };

        _logger.LogInformation(
            "Starting generic sync batch processing. DeviceId: {DeviceId}, Items: {Count}, SyncTimestamp: {Timestamp}",
            input.DeviceId,
            input.Items.Count,
            input.SyncTimestamp);

        foreach (var item in input.Items)
        {
            var result = await ProcessGenericSyncItemAsync(item);
            response.Results.Add(result);

            if (result.Success)
            {
                response.SuccessCount++;
                if (result.ConflictResolution.HasValue)
                {
                    response.ConflictCount++;
                }

                // Track by type
                if (!response.SyncedByType.ContainsKey(item.EntityType))
                {
                    response.SyncedByType[item.EntityType] = 0;
                }
                response.SyncedByType[item.EntityType]++;
            }
            else
            {
                response.FailedCount++;
            }
        }

        _logger.LogInformation(
            "Generic sync batch completed. Success: {Success}, Failed: {Failed}, Conflicts: {Conflicts}, Types: {Types}",
            response.SuccessCount,
            response.FailedCount,
            response.ConflictCount,
            string.Join(", ", response.SyncedByType.Select(kv => $"{kv.Key}:{kv.Value}")));

        return response;
    }

    /// <inheritdoc />
    public async Task<GenericServerChangesResponseDto> GetEntityChangesAsync(GetEntityChangesDto input)
    {
        var response = new GenericServerChangesResponseDto
        {
            EntityType = input.EntityType,
            Changes = new List<object>(),
            DeletedIds = new List<Guid>(),
            ServerTimestamp = DateTime.UtcNow
        };

        // For now, only TestRequest is fully supported for server changes
        if (input.EntityType == SyncEntityType.TestRequest)
        {
            var testRequestChanges = await GetServerChangesAsync(new GetServerChangesDto
            {
                LastSyncTimestamp = input.LastSyncTimestamp,
                DeviceId = input.DeviceId,
                MaxResults = input.MaxResults
            });

            response.Changes = testRequestChanges.Changes.Cast<object>().ToList();
            response.DeletedIds = testRequestChanges.DeletedIds;
            response.HasMore = testRequestChanges.HasMore;
            response.TotalCount = testRequestChanges.TotalCount;
        }
        else
        {
            _logger.LogWarning(
                "GetEntityChanges not fully implemented for entity type: {EntityType}",
                input.EntityType);
        }

        return response;
    }

    /// <summary>
    /// Process a single generic sync item
    /// </summary>
    private async Task<GenericSyncResultDto> ProcessGenericSyncItemAsync(GenericSyncItemDto item)
    {
        var result = new GenericSyncResultDto
        {
            ClientId = item.ClientId,
            EntityType = item.EntityType
        };

        var handler = _handlerRegistry.GetHandler(item.EntityType);
        if (handler == null)
        {
            result.Success = false;
            result.Error = $"No sync handler registered for entity type: {item.EntityType}";
            _logger.LogWarning("No sync handler for entity type: {EntityType}", item.EntityType);
            return result;
        }

        try
        {
            var offlinePrincipal = await BuildOfflineUserPrincipalAsync(item.OfflineUserId, item.ClientId);
            using var offlineUser = offlinePrincipal != null ? CurrentPrincipalAccessor.Change(offlinePrincipal) : null;
            switch (item.OperationType)
            {
                case SyncOperationType.Create:
                    result = await ProcessGenericCreateAsync(handler, item);
                    break;

                case SyncOperationType.Update:
                    result = await ProcessGenericUpdateAsync(handler, item);
                    break;

                case SyncOperationType.Delete:
                    result = await ProcessGenericDeleteAsync(handler, item);
                    break;

                default:
                    result.Success = false;
                    result.Error = $"Unknown operation type: {item.OperationType}";
                    break;
            }

            // Sửa/xoá lưu khi kết thúc request → lưu ngay khi còn danh tính người tạo offline để ghi đúng người sửa
            if (offlineUser != null && CurrentUnitOfWork != null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }
        }
        catch (AbpValidationException ex)
        {
            result.Success = false;
            result.Error = "Validation failed";
            result.ValidationErrors = ex.ValidationErrors
                .Select(e => $"{e.MemberNames.FirstOrDefault()}: {e.ErrorMessage}")
                .ToList();

            _logger.LogWarning(
                "Sync validation failed for ClientId: {ClientId}, EntityType: {EntityType}. Errors: {Errors}",
                item.ClientId,
                item.EntityType,
                string.Join(", ", result.ValidationErrors));
        }
        catch (TestRequestValidationException ex)
        {
            // Message gốc chỉ là "Dữ liệu không hợp lệ" → trả nội dung lỗi từng trường để người dùng biết vì sao
            result.Success = false;
            result.Error = string.Join("; ", ex.Errors.Values);

            _logger.LogWarning(
                "Sync validation failed for ClientId: {ClientId}, EntityType: {EntityType}. Errors: {Errors}",
                item.ClientId,
                item.EntityType,
                result.Error);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;

            _logger.LogError(ex,
                "Sync failed for ClientId: {ClientId}, EntityType: {EntityType}, OperationType: {OperationType}",
                item.ClientId,
                item.EntityType,
                item.OperationType);
        }

        return result;
    }

    private async Task<GenericSyncResultDto> ProcessGenericCreateAsync(ISyncEntityHandler handler, GenericSyncItemDto item)
    {
        var result = new GenericSyncResultDto
        {
            ClientId = item.ClientId,
            EntityType = item.EntityType
        };

        // Cùng 1 item create có thể bị gửi nhiều lần (nhiều tab, retry sau khi mất response).
        // Khóa theo ClientId + nhớ ServerId đã tạo → lần gửi lặp trả về bản ghi cũ thay vì tạo bản trùng.
        await using var lockHandle = await DistributedLock.TryAcquireAsync(
            $"sync-create:{item.ClientId}",
            TimeSpan.FromSeconds(30));
        if (lockHandle == null)
        {
            result.Success = false;
            result.Error = "Dữ liệu đang được đồng bộ từ phiên khác, vui lòng thử lại sau";
            return result;
        }

        var cacheKey = $"{item.EntityType}:{item.ClientId}";
        var created = await SyncCreatedCache.GetAsync(cacheKey);
        if (created != null && await handler.ExistsAsync(created.ServerId))
        {
            return await BuildAlreadyCreatedResultAsync(handler, item, created.ServerId);
        }

        // Handler dùng ClientId làm Id: kiểm tra trong DB → bền vững qua BE restart / nhiều replica (cache + lock chỉ trong RAM)
        Guid? clientGuid = handler.UsesClientIdAsServerId && Guid.TryParse(item.ClientId, out var parsedClientId)
            ? parsedClientId
            : null;
        if (clientGuid.HasValue && await handler.ExistsAsync(clientGuid.Value))
        {
            return await BuildAlreadyCreatedResultAsync(handler, item, clientGuid.Value);
        }

        Guid serverId;
        object serverData;
        try
        {
            // UoW riêng, commit ngay khi còn giữ khóa: request gửi lặp chờ khóa xong sẽ thấy bản ghi đã tạo;
            // lỗi DB của item này cũng không làm hỏng cả batch (trước đây cả request trả 500)
            using (clientGuid.HasValue ? SyncCreateContext.UsePreferredId(clientGuid.Value) : null)
            using (var uow = UnitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
            {
                (serverId, serverData) = await handler.CreateAsync(item.Data);
                await uow.CompleteAsync();
            }
        }
        catch (Exception) when (clientGuid.HasValue)
        {
            // Request trùng chạy song song (replica khác) đã tạo trước → insert đụng khóa chính → trả lại bản ghi đó
            if (await handler.ExistsAsync(clientGuid.Value))
            {
                return await BuildAlreadyCreatedResultAsync(handler, item, clientGuid.Value);
            }
            throw;
        }

        await SyncCreatedCache.SetAsync(
            cacheKey,
            new SyncCreatedCacheItem { ServerId = serverId },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) });

        result.Success = true;
        result.ServerId = serverId;
        result.ServerData = serverData;

        _logger.LogInformation(
            "Generic sync create succeeded. ClientId: {ClientId}, EntityType: {EntityType}, ServerId: {ServerId}",
            item.ClientId,
            item.EntityType,
            serverId);

        return result;
    }

    private async Task<GenericSyncResultDto> BuildAlreadyCreatedResultAsync(
        ISyncEntityHandler handler,
        GenericSyncItemDto item,
        Guid serverId)
    {
        _logger.LogInformation(
            "Generic sync create skipped (already created). ClientId: {ClientId}, EntityType: {EntityType}, ServerId: {ServerId}",
            item.ClientId,
            item.EntityType,
            serverId);

        return new GenericSyncResultDto
        {
            ClientId = item.ClientId,
            EntityType = item.EntityType,
            Success = true,
            ServerId = serverId,
            ServerData = await handler.GetAsync(serverId)
        };
    }

    private async Task<GenericSyncResultDto> ProcessGenericUpdateAsync(ISyncEntityHandler handler, GenericSyncItemDto item)
    {
        var result = new GenericSyncResultDto
        {
            ClientId = item.ClientId,
            ServerId = item.ServerId,
            EntityType = item.EntityType
        };

        if (!item.ServerId.HasValue)
        {
            result.Success = false;
            result.Error = "ServerId is required for update operations";
            return result;
        }

        if (!await handler.ExistsAsync(item.ServerId.Value))
        {
            result.Success = false;
            result.Error = $"Entity with ID {item.ServerId} not found";
            return result;
        }

        // Conflict detection - "last write wins":
        // server sửa sau thời điểm client sửa offline → giữ bản server, FE báo cho người dùng biết bản offline bị bỏ qua
        var serverLastModified = await handler.GetLastModificationTimeAsync(item.ServerId.Value);
        var serverIsNewer = serverLastModified.HasValue && item.ClientLastModified < serverLastModified.Value;

        if (serverIsNewer)
        {
            result.ConflictResolution = SyncConflictResolution.ServerWins;
            result.Success = true;
            result.ServerData = await handler.GetAsync(item.ServerId.Value);

            _logger.LogInformation(
                "Generic sync conflict resolved: ServerWins. ClientId: {ClientId}, EntityType: {EntityType}",
                item.ClientId,
                item.EntityType);
        }
        else
        {
            // No conflict
            var serverData = await handler.UpdateAsync(item.ServerId.Value, item.Data);
            result.Success = true;
            result.ServerData = serverData;

            _logger.LogInformation(
                "Generic sync update succeeded (no conflict). ClientId: {ClientId}, EntityType: {EntityType}",
                item.ClientId,
                item.EntityType);
        }

        return result;
    }

    private async Task<GenericSyncResultDto> ProcessGenericDeleteAsync(ISyncEntityHandler handler, GenericSyncItemDto item)
    {
        var result = new GenericSyncResultDto
        {
            ClientId = item.ClientId,
            ServerId = item.ServerId,
            EntityType = item.EntityType
        };

        if (!item.ServerId.HasValue)
        {
            result.Success = false;
            result.Error = "ServerId is required for delete operations";
            return result;
        }

        await handler.DeleteAsync(item.ServerId.Value);
        result.Success = true;

        _logger.LogInformation(
            "Generic sync delete succeeded. ClientId: {ClientId}, EntityType: {EntityType}, ServerId: {ServerId}",
            item.ClientId,
            item.EntityType,
            item.ServerId);

        return result;
    }
}
