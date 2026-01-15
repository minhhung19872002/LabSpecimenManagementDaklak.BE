using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Validation;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Partial class for generic sync methods
/// </summary>
public partial class SyncAppService
{
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

        var (serverId, serverData) = await handler.CreateAsync(item.Data);

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

        // Conflict detection
        var serverLastModified = await handler.GetLastModificationTimeAsync(item.ServerId.Value);
        var conflict = serverLastModified.HasValue && item.ClientLastModified < serverLastModified.Value;

        if (conflict)
        {
            // "Last write wins" - compare timestamps
            if (item.ClientLastModified > serverLastModified)
            {
                // Client wins
                result.ConflictResolution = SyncConflictResolution.ClientWins;
                var serverData = await handler.UpdateAsync(item.ServerId.Value, item.Data);
                result.Success = true;
                result.ServerData = serverData;

                _logger.LogInformation(
                    "Generic sync conflict resolved: ClientWins. ClientId: {ClientId}, EntityType: {EntityType}",
                    item.ClientId,
                    item.EntityType);
            }
            else
            {
                // Server wins
                result.ConflictResolution = SyncConflictResolution.ServerWins;
                result.Success = true;
                result.ServerData = await handler.GetAsync(item.ServerId.Value);

                _logger.LogInformation(
                    "Generic sync conflict resolved: ServerWins. ClientId: {ClientId}, EntityType: {EntityType}",
                    item.ClientId,
                    item.EntityType);
            }
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
