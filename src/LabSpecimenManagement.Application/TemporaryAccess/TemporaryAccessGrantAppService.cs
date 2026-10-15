using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.TemporaryAccess;

[Authorize(LabSpecimenManagementPermissions.TemporaryAccess.Default)]
public class TemporaryAccessGrantAppService : ApplicationService, ITemporaryAccessGrantAppService
{
    private readonly IRepository<TemporaryAccessGrant, Guid> _repository;
    private readonly IPermissionGrantRepository _permissionGrantRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly INotificationHubService _notificationHubService;

    public TemporaryAccessGrantAppService(
        IRepository<TemporaryAccessGrant, Guid> repository,
        IPermissionGrantRepository permissionGrantRepository,
        IdentityUserManager userManager,
        IIdentityUserRepository identityUserRepository,
        INotificationHubService notificationHubService)
    {
        _repository = repository;
        _permissionGrantRepository = permissionGrantRepository;
        _userManager = userManager;
        _identityUserRepository = identityUserRepository;
        _notificationHubService = notificationHubService;
    }

    public async Task<PagedResultDto<TemporaryAccessGrantDto>> GetListAsync(TemporaryAccessGrantPagedRequestDto input)
    {
        var query = await _repository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.ToLower();
            query = query.Where(x => x.EmployeeFullName.ToLower().Contains(filter)
                || (x.EmployeeEmail != null && x.EmployeeEmail.ToLower().Contains(filter)));
        }

        if (input.Status.HasValue)
            query = query.Where(x => x.Status == input.Status.Value);

        var total = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(query
            .OrderByDescending(x => x.ExpiryDate)
            .ThenByDescending(x => x.CreationTime)  // M2: deterministic pagination
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        return new PagedResultDto<TemporaryAccessGrantDto>(total, items.Select(MapToDto).ToList());
    }

    public async Task<TemporaryAccessGrantDto> GetAsync(Guid id)
    {
        var grant = await _repository.GetAsync(id);
        return MapToDto(grant);
    }

    [Authorize(LabSpecimenManagementPermissions.TemporaryAccess.Create)]
    [UnitOfWork]  // C4: đảm bảo external user + grant record rollback cùng nhau nếu fail
    public async Task<TemporaryAccessGrantDto> CreateAsync(CreateTemporaryAccessGrantDto input)
    {
        var startDate = input.StartDate.Date;
        var expiryDate = VietnamTime.EndOfDayToServerTime(input.ExpiryDate); // 23:59:59 giờ VN

        // W3: Không cho phép ngày phân công trong quá khứ
        if (startDate < VietnamTime.Today)
            throw new UserFriendlyException("Ngày phân công không được nhỏ hơn ngày hôm nay");

        if (input.ExpiryDate.Date < startDate)
            throw new UserFriendlyException("Ngày hết hiệu lực không được nhỏ hơn ngày phân công");

        Guid userId;
        var grant = new TemporaryAccessGrant
        {
            EmployeeType = input.EmployeeType,
            StartDate = startDate,
            ExpiryDate = expiryDate,
            SelectedModules = JsonSerializer.Serialize(input.SelectedModules),
            Reason = input.Reason
        };

        if (input.EmployeeType == TemporaryEmployeeType.Internal)
        {
            if (!input.UserId.HasValue)
                throw new UserFriendlyException("Vui lòng chọn nhân viên");

            userId = input.UserId.Value;
            var internalUser = await _identityUserRepository.FindAsync(userId)
                ?? throw new UserFriendlyException("Không tìm thấy nhân viên");

            grant.UserId = userId;
            var fullName = $"{internalUser.Surname} {internalUser.Name}".Trim();
            grant.EmployeeFullName = string.IsNullOrWhiteSpace(fullName) ? internalUser.UserName : fullName;
            grant.EmployeeEmail = internalUser.Email;
            grant.EmployeePhone = internalUser.PhoneNumber;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.ExternalUsername))
                throw new UserFriendlyException("Tên đăng nhập không được để trống");
            if (string.IsNullOrWhiteSpace(input.ExternalFullName))
                throw new UserFriendlyException("Họ và tên không được để trống");
            if (string.IsNullOrWhiteSpace(input.ExternalPassword))
                throw new UserFriendlyException("Mật khẩu không được để trống");
            if (input.ExternalPassword != input.ExternalConfirmPassword)
                throw new UserFriendlyException("Mật khẩu xác nhận không khớp");

            // W2: Server-side password complexity validation
            ValidatePasswordComplexity(input.ExternalPassword);

            // Check for duplicate username before CreateAsync — avoids ABP's English identity errors
            if (await _userManager.FindByNameAsync(input.ExternalUsername) != null)
                throw new UserFriendlyException($"Tên đăng nhập '{input.ExternalUsername}' đã được sử dụng. Vui lòng chọn tên đăng nhập khác.");

            // Create new external account — ABP requires a non-null email, use placeholder if none provided
            var accountEmail = !string.IsNullOrWhiteSpace(input.ExternalEmail)
                ? input.ExternalEmail
                : $"{input.ExternalUsername}@temp.internal";
            var externalUser = new IdentityUser(GuidGenerator.Create(), input.ExternalUsername, accountEmail, CurrentTenant.Id);
            externalUser.Name = input.ExternalFullName;
            if (!string.IsNullOrWhiteSpace(input.ExternalPhone))
                externalUser.SetPhoneNumber(input.ExternalPhone, false);

            var createResult = await _userManager.CreateAsync(externalUser, input.ExternalPassword);
            if (!createResult.Succeeded)
                throw new UserFriendlyException(string.Join(", ", createResult.Errors.Select(e => e.Description)));

            externalUser.SetProperty("AccountExpirationDate", expiryDate);
            externalUser.SetProperty("IsTemporaryExternalAccount", true);
            await _userManager.UpdateAsync(externalUser);

            userId = externalUser.Id;
            grant.ExternalUserId = externalUser.Id;
            grant.ExternalUsername = input.ExternalUsername;
            grant.EmployeeFullName = input.ExternalFullName;
            grant.EmployeeEmail = input.ExternalEmail;
            grant.EmployeePhone = input.ExternalPhone;
        }

        var granted = await GrantPermissionsAsync(userId, input.SelectedModules, Guid.Empty);
        grant.GrantedPermissions = JsonSerializer.Serialize(granted);

        // Denormalize granter's display name for audit tracing
        var grantedByFullName = $"{CurrentUser.SurName} {CurrentUser.Name}".Trim();
        grant.GrantedByName = string.IsNullOrWhiteSpace(grantedByFullName)
            ? CurrentUser.UserName
            : grantedByFullName;

        await _repository.InsertAsync(grant, autoSave: true);
        return MapToDto(grant);
    }

    [Authorize(LabSpecimenManagementPermissions.TemporaryAccess.Edit)]
    [UnitOfWork]  // C3: đảm bảo revoke + grant + update là atomic
    public async Task<TemporaryAccessGrantDto> UpdateAsync(Guid id, UpdateTemporaryAccessGrantDto input)
    {
        var grant = await _repository.GetAsync(id);
        if (grant.Status == GrantStatus.Expired)
            throw new UserFriendlyException("Không thể sửa lịch trực đã hết hạn");

        var startDate = input.StartDate.Date;
        var expiryDate = VietnamTime.EndOfDayToServerTime(input.ExpiryDate); // 23:59:59 giờ VN

        // Grant may have already started — only enforce ordering
        if (input.ExpiryDate.Date < startDate)
            throw new UserFriendlyException("Ngày hết hiệu lực không được nhỏ hơn ngày phân công");
        if (input.ExpiryDate.Date < VietnamTime.Today)
            throw new UserFriendlyException("Ngày hết hạn không được nhỏ hơn ngày hôm nay");

        var userId = grant.EmployeeType == TemporaryEmployeeType.Internal
            ? grant.UserId!.Value
            : grant.ExternalUserId!.Value;

        // Save old owned perms BEFORE modifying (for revocation after update)
        var oldGrantedPermissionsJson = grant.GrantedPermissions ?? "[]";

        var allNewPerms = TemporaryAccessPermissionMapper.GetPermissionsForModules(input.SelectedModules);

        // Insert missing perms and compute co-owned set for new selection
        var newOwned = await GrantPermissionsAsync(userId, input.SelectedModules, grant.Id);

        // Revoke previously owned perms no longer needed — protected by new selection and other active grants
        await RevokeGrantedPermissionsAsync(userId, oldGrantedPermissionsJson, grant.Id, allNewPerms);

        grant.GrantedPermissions = JsonSerializer.Serialize(newOwned);
        grant.SelectedModules = JsonSerializer.Serialize(input.SelectedModules);
        grant.StartDate = startDate;
        grant.ExpiryDate = expiryDate;
        grant.Reason = input.Reason;

        if (grant.EmployeeType == TemporaryEmployeeType.External && grant.ExternalUserId.HasValue)
        {
            var externalUser = await _userManager.FindByIdAsync(grant.ExternalUserId.Value.ToString());
            if (externalUser != null)
            {
                externalUser.SetProperty("AccountExpirationDate", expiryDate);
                await _userManager.UpdateAsync(externalUser);
            }
        }

        await _repository.UpdateAsync(grant, autoSave: true);
        return MapToDto(grant);
    }

    [Authorize(LabSpecimenManagementPermissions.TemporaryAccess.Expire)]
    [UnitOfWork]
    public async Task ExpireAsync(Guid id)
    {
        var grant = await _repository.GetAsync(id);
        if (grant.Status == GrantStatus.Expired)
            throw new UserFriendlyException("Lịch trực này đã hết hạn");

        var userId = grant.EmployeeType == TemporaryEmployeeType.Internal
            ? grant.UserId!.Value
            : grant.ExternalUserId!.Value;

        await RevokeGrantedPermissionsAsync(userId, grant.GrantedPermissions ?? "[]", grant.Id);

        if (grant.EmployeeType == TemporaryEmployeeType.External && grant.ExternalUserId.HasValue)
        {
            var externalUser = await _userManager.FindByIdAsync(grant.ExternalUserId.Value.ToString());
            if (externalUser != null)
            {
                externalUser.SetIsActive(false);
                await _userManager.UpdateAsync(externalUser);
            }

            // Notify the external user's active session to force logout
            await _notificationHubService.NotifyAccountExpiredAsync(grant.ExternalUserId.Value);
        }

        grant.Status = GrantStatus.Expired;
        await _repository.UpdateAsync(grant, autoSave: true);
    }

    public async Task<TemporaryAccessGrantStatsDto> GetStatsAsync()
    {
        var query = await _repository.GetQueryableAsync();
        var statuses = await AsyncExecuter.ToListAsync(query.Select(g => g.Status));
        var active = statuses.Count(s => s == GrantStatus.Active);
        return new TemporaryAccessGrantStatsDto
        {
            Total = statuses.Count,
            Active = active,
            Expired = statuses.Count - active,
        };
    }

    // --- Helpers ---

    // W2: Explicit password complexity check (mirrors FE schema rules)
    private static void ValidatePasswordComplexity(string password)
    {
        if (password.Length < 6)
            throw new UserFriendlyException("Mật khẩu phải có ít nhất 6 ký tự");
        if (!password.Any(char.IsUpper))
            throw new UserFriendlyException("Mật khẩu phải có ít nhất 1 chữ hoa");
        if (!password.Any(char.IsLower))
            throw new UserFriendlyException("Mật khẩu phải có ít nhất 1 chữ thường");
        if (!password.Any(char.IsDigit))
            throw new UserFriendlyException("Mật khẩu phải có ít nhất 1 chữ số");
        if (!password.Any(c => "!@#$%^&*(),.?\":{}|<>".Contains(c)))
            throw new UserFriendlyException("Mật khẩu phải có ít nhất 1 ký tự đặc biệt");
    }

    private async Task<List<string>> GrantPermissionsAsync(Guid userId, List<string> selectedModules, Guid currentGrantId)
    {
        var allPermissionNames = TemporaryAccessPermissionMapper.GetPermissionsForModules(selectedModules).ToList();
        if (!allPermissionNames.Any())
            return new();

        var existingGrants = await _permissionGrantRepository.GetListAsync(
            UserPermissionValueProvider.ProviderName, userId.ToString());
        var existingNames = existingGrants.Select(g => g.Name).ToHashSet();

        var toInsert = allPermissionNames.Where(p => !existingNames.Contains(p)).ToList();

        foreach (var permName in toInsert)
        {
            await _permissionGrantRepository.InsertAsync(
                new PermissionGrant(GuidGenerator.Create(), permName,
                    UserPermissionValueProvider.ProviderName, userId.ToString(), CurrentTenant.Id),
                autoSave: false);
        }

        if (toInsert.Any())
            await CurrentUnitOfWork.SaveChangesAsync();

        // Co-ownership: perms already in DB that are covered by another active (non-expired) temp grant
        // also belong to this grant — so they get revoked when this grant eventually expires.
        // Perms that exist only from admin/role grants are NOT included (they must survive expiry).
        var allGrantsQuery = await _repository.GetQueryableAsync();
        var otherActiveGrants = await AsyncExecuter.ToListAsync(
            allGrantsQuery.Where(g =>
                g.Id != currentGrantId &&
                g.Status == GrantStatus.Active &&
                g.ExpiryDate >= DateTime.Now &&
                (g.UserId == userId || g.ExternalUserId == userId)));

        var otherTempGrantPerms = new HashSet<string>();
        foreach (var g in otherActiveGrants)
        {
            List<string> otherModules;
            try { otherModules = JsonSerializer.Deserialize<List<string>>(g.SelectedModules) ?? new(); }
            catch (JsonException) { otherModules = new(); }
            foreach (var p in TemporaryAccessPermissionMapper.GetPermissionsForModules(otherModules))
                otherTempGrantPerms.Add(p);
        }

        // owned = newly inserted by this call ∪ pre-existing perms shared with another active temp grant
        return allPermissionNames
            .Where(p => !existingNames.Contains(p) || otherTempGrantPerms.Contains(p))
            .ToList();
    }

    private async Task RevokeGrantedPermissionsAsync(
        Guid userId, string grantedPermissionsJson, Guid currentGrantId,
        IEnumerable<string>? currentGrantNewPerms = null)
    {
        List<string> grantedPerms;
        try
        {
            grantedPerms = JsonSerializer.Deserialize<List<string>>(grantedPermissionsJson ?? "[]") ?? new();
        }
        catch (JsonException)
        {
            return;
        }

        if (!grantedPerms.Any()) return;

        var allGrantsQuery = await _repository.GetQueryableAsync();
        var otherActiveGrants = await AsyncExecuter.ToListAsync(
            allGrantsQuery.Where(g =>
                g.Id != currentGrantId &&
                g.Status == GrantStatus.Active &&
                g.ExpiryDate >= DateTime.Now &&
                (g.UserId == userId || g.ExternalUserId == userId)));

        var stillNeededPerms = new HashSet<string>(currentGrantNewPerms ?? Enumerable.Empty<string>());
        foreach (var otherGrant in otherActiveGrants)
        {
            List<string> otherModules;
            try { otherModules = JsonSerializer.Deserialize<List<string>>(otherGrant.SelectedModules) ?? new(); }
            catch (JsonException) { otherModules = new(); }

            foreach (var p in TemporaryAccessPermissionMapper.GetPermissionsForModules(otherModules))
                stillNeededPerms.Add(p);
        }

        var toRevoke = grantedPerms.Where(p => !stillNeededPerms.Contains(p)).ToList();
        if (!toRevoke.Any()) return;

        var userGrants = await _permissionGrantRepository.GetListAsync(
            UserPermissionValueProvider.ProviderName, userId.ToString());

        var deleted = false;
        foreach (var permName in toRevoke)
        {
            var match = userGrants.FirstOrDefault(g => g.Name == permName);
            if (match != null)
            {
                await _permissionGrantRepository.DeleteAsync(match, autoSave: false);
                deleted = true;
            }
        }
        if (deleted)
            await CurrentUnitOfWork.SaveChangesAsync();
    }

    private static TemporaryAccessGrantDto MapToDto(TemporaryAccessGrant grant)
    {
        var modules = new List<string>();
        try { modules = JsonSerializer.Deserialize<List<string>>(grant.SelectedModules) ?? new(); } catch (JsonException) { }

        return new TemporaryAccessGrantDto
        {
            Id = grant.Id,
            EmployeeType = grant.EmployeeType,
            UserId = grant.UserId,
            ExternalUserId = grant.ExternalUserId,
            ExternalUsername = grant.ExternalUsername,
            EmployeeFullName = grant.EmployeeFullName,
            EmployeeEmail = grant.EmployeeEmail,
            EmployeePhone = grant.EmployeePhone,
            StartDate = grant.StartDate,
            ExpiryDate = grant.ExpiryDate,
            Status = grant.Status,
            SelectedModules = modules,
            Reason = grant.Reason,
            CreationTime = grant.CreationTime,
            GrantedById = grant.CreatorId,
            GrantedByName = grant.GrantedByName
        };
    }
}
