using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.TemporaryAccess;
using Microsoft.Extensions.Logging;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job to expire temporary access grants whose ExpiryDate has passed.
/// Runs hourly. Bypasses app service auth — operates directly on repositories.
/// </summary>
public class TemporaryAccessGrantExpirationJob : ITransientDependency
{
    private readonly IRepository<TemporaryAccessGrant, Guid> _repository;
    private readonly IPermissionGrantRepository _permissionGrantRepository;
    private readonly IdentityUserManager _userManager;
    private readonly INotificationHubService _notificationHubService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<TemporaryAccessGrantExpirationJob> _logger;

    public TemporaryAccessGrantExpirationJob(
        IRepository<TemporaryAccessGrant, Guid> repository,
        IPermissionGrantRepository permissionGrantRepository,
        IdentityUserManager userManager,
        INotificationHubService notificationHubService,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<TemporaryAccessGrantExpirationJob> logger)
    {
        _repository = repository;
        _permissionGrantRepository = permissionGrantRepository;
        _userManager = userManager;
        _notificationHubService = notificationHubService;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting temporary access grant expiration job...");

        try
        {
            var now = DateTime.Now;
            var expiredGrants = await _repository.GetListAsync(
                g => g.Status == GrantStatus.Active && g.ExpiryDate <= now);

            if (expiredGrants.Count == 0)
            {
                _logger.LogInformation("Temporary access expiration job: no expired grants found.");
                return;
            }

            var expiredCount = 0;
            foreach (var grant in expiredGrants)
            {
                // Track which external user to notify — determined inside UoW, notified after commit
                Guid? externalUserToNotify = null;
                try
                {
                    using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
                    externalUserToNotify = await ExpireGrantAsync(grant);
                    await uow.CompleteAsync();
                    expiredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to expire grant {GrantId} for {EmployeeName}", grant.Id, grant.EmployeeFullName);
                    continue;
                }

                // Send SignalR AFTER successful commit — avoids false notification on rollback
                if (externalUserToNotify.HasValue)
                {
                    try
                    {
                        await _notificationHubService.NotifyAccountExpiredAsync(externalUserToNotify.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "SignalR AccountExpired notification failed for user {UserId}", externalUserToNotify);
                    }
                }
            }

            _logger.LogInformation("Temporary access expiration job completed. Expired {Count}/{Total} grants.", expiredCount, expiredGrants.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing temporary access grant expiration job");
            throw;
        }
    }

    /// <summary>
    /// Expires a single grant. Returns the external UserId to notify via SignalR (null if internal).
    /// Must be called inside an active UoW.
    /// </summary>
    private async Task<Guid?> ExpireGrantAsync(TemporaryAccessGrant grant)
    {
        var userId = grant.EmployeeType == TemporaryEmployeeType.Internal
            ? grant.UserId!.Value
            : grant.ExternalUserId!.Value;

        // Smart revoke: only remove permissions not needed by other active grants for this user
        await RevokePermissionsAsync(userId, grant.GrantedPermissions ?? "[]", grant.Id);

        Guid? externalUserToNotify = null;
        if (grant.EmployeeType == TemporaryEmployeeType.External && grant.ExternalUserId.HasValue)
        {
            var externalUser = await _userManager.FindByIdAsync(grant.ExternalUserId.Value.ToString());
            if (externalUser != null)
            {
                externalUser.SetIsActive(false);
                await _userManager.UpdateAsync(externalUser);
            }
            externalUserToNotify = grant.ExternalUserId.Value;
        }

        grant.Status = GrantStatus.Expired;
        await _repository.UpdateAsync(grant, autoSave: false); // flushed by uow.CompleteAsync()
        return externalUserToNotify;
    }

    /// <summary>
    /// Revokes permissions from the expired grant, skipping any still required by other active grants.
    /// Mirrors TemporaryAccessGrantAppService.RevokeGrantedPermissionsAsync smart-revoke logic.
    /// </summary>
    private async Task RevokePermissionsAsync(Guid userId, string grantedPermissionsJson, Guid currentGrantId)
    {
        List<string> grantedPerms;
        try { grantedPerms = JsonSerializer.Deserialize<List<string>>(grantedPermissionsJson) ?? new(); }
        catch (JsonException) { return; }

        if (!grantedPerms.Any()) return;

        // Collect permissions still required by other active grants for this user
        var otherActiveGrants = await _repository.GetListAsync(g =>
            g.Id != currentGrantId &&
            g.Status == GrantStatus.Active &&
            g.ExpiryDate >= DateTime.Now &&
            (g.UserId == userId || g.ExternalUserId == userId));

        var stillNeededPerms = new HashSet<string>();
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

        // Single batch flush instead of per-delete round trips
        if (deleted)
            await _unitOfWorkManager.Current!.SaveChangesAsync();
    }
}
