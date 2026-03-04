using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Data;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Partial class for Department Supporter Management operations
/// Người hỗ trợ khoa phòng - có quyền truy cập TẤT CẢ công việc trong khoa phòng, có thời hạn
/// </summary>
public partial class LaboratoryTaskAppService
{
    // Vietnam timezone offset (UTC+7)
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    #region Department Supporter Management

    [Authorize]
    public async Task<List<LaboratoryDepartmentSupporterDto>> GetDepartmentSupportersAsync()
    {
        var labIds = await GetAccessibleLaboratoryIdsAsync();
        if (!labIds.Any())
        {
            return new List<LaboratoryDepartmentSupporterDto>();
        }

        var supporters = await _departmentSupporterRepository.GetListAsync(
            x => labIds.Contains(x.LaboratoryId) && x.ExpirationDate > DateTime.UtcNow);

        // Batch fetch user names
        var userNames = await _userNameProvider.GetUserNamesAsync(supporters.Select(s => (Guid?)s.UserId));

        // Get user emails
        var userIds = supporters.Select(s => s.UserId).Distinct().ToList();
        var users = await _userRepository.GetListAsync();
        var userDict = users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id);

        // Group by UserId - show 1 record per user (take first Id for delete reference, max expiration)
        var dtos = supporters
            .GroupBy(s => s.UserId)
            .Select(g =>
            {
                var first = g.First();
                return new LaboratoryDepartmentSupporterDto
                {
                    Id = first.Id,
                    LaboratoryId = first.LaboratoryId,
                    UserId = g.Key,
                    FullName = userNames.TryGetValue(g.Key, out var name) ? name : null,
                    Email = userDict.TryGetValue(g.Key, out var user) ? user.Email : null,
                    UserName = userDict.TryGetValue(g.Key, out var u) ? u.UserName : null,
                    ExpirationDate = g.Max(s => s.ExpirationDate),
                    CreationTime = first.CreationTime
                };
            })
            .ToList();

        return dtos.OrderBy(x => x.FullName).ToList();
    }

    [Authorize]
    public async Task<List<LaboratoryDepartmentSupporterDto>> AddDepartmentSupportersAsync(AddDepartmentSupportersDto input)
    {
        var currentUserId = CurrentUser.Id;

        var labIds = await GetAccessibleLaboratoryIdsAsync();
        if (!labIds.Any())
        {
            throw new Volo.Abp.BusinessException(message: "Không tìm thấy khoa phòng nào trong hệ thống");
        }

        // Get existing department supporters for ALL accessible labs
        var existingSupporters = await _departmentSupporterRepository.GetListAsync(
            x => labIds.Contains(x.LaboratoryId));
        // Key: (LaboratoryId, UserId)
        var existingMap = existingSupporters
            .ToDictionary(x => (x.LaboratoryId, x.UserId));

        foreach (var entry in input.Entries)
        {
            // Skip if user is the current user (QLKT)
            if (entry.UserId == currentUserId!.Value)
            {
                continue;
            }

            // Compute end-of-day in Vietnam timezone → UTC
            // Ensure DateTime.Kind is Unspecified so DateTimeOffset accepts VietnamOffset
            var expirationDateVn = DateTime.SpecifyKind(entry.ExpirationDate.Date.AddDays(1), DateTimeKind.Unspecified);
            var expirationDateUtc = new DateTimeOffset(expirationDateVn, VietnamOffset).UtcDateTime;

            // Add supporter to ALL accessible labs
            foreach (var labId in labIds)
            {
                if (existingMap.TryGetValue((labId, entry.UserId), out var existingSupporter))
                {
                    existingSupporter.ExpirationDate = expirationDateUtc;
                    await _departmentSupporterRepository.UpdateAsync(existingSupporter);
                }
                else
                {
                    var supporter = new LaboratoryDepartmentSupporter(
                        GuidGenerator.Create(),
                        labId,
                        entry.UserId,
                        expirationDateUtc
                    );
                    supporter.CreatorId = currentUserId;
                    await _departmentSupporterRepository.InsertAsync(supporter);
                }
            }
        }

        return await GetDepartmentSupportersAsync();
    }

    [Authorize]
    public async Task RemoveDepartmentSupporterAsync(Guid supporterId)
    {
        var supporter = await _departmentSupporterRepository.FindAsync(supporterId);
        if (supporter == null)
        {
            throw new EntityNotFoundException("DepartmentSupporter", supporterId);
        }

        // Delete ALL records for this user across all labs (since we add to all labs)
        var allRecords = await _departmentSupporterRepository.GetListAsync(
            x => x.UserId == supporter.UserId);
        foreach (var record in allRecords)
        {
            await _departmentSupporterRepository.DeleteAsync(record);
        }
    }

    /// <summary>
    /// Xóa department supporters đã hết hạn (gọi bởi cronjob)
    /// </summary>
    public async Task<int> RemoveExpiredDepartmentSupportersAsync()
    {
        var expiredSupporters = await _departmentSupporterRepository.GetListAsync(
            x => x.ExpirationDate <= DateTime.UtcNow);

        if (!expiredSupporters.Any())
        {
            return 0;
        }

        foreach (var supporter in expiredSupporters)
        {
            await _departmentSupporterRepository.DeleteAsync(supporter);
        }

        return expiredSupporters.Count;
    }

    /// <summary>
    /// Helper: Get laboratory IDs managed by current user
    /// Check cả Laboratory.TechnicalManagerId và LaboratoryTask.TechnicalManagerId
    /// </summary>
    private async Task<List<Guid>> GetManagedLaboratoryIdsAsync()
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue) return new List<Guid>();

        // Labs where user is TechnicalManager of the lab itself
        var labs = await _laboratoryRepository.GetListAsync();
        var labIds = labs
            .Where(l => l.TechnicalManagerId == currentUserId.Value)
            .Select(l => l.Id)
            .ToHashSet();

        // Also get lab IDs from tasks where user is QLKT
        var tasks = await _laboratoryTaskRepository.GetListAsync(
            t => t.TechnicalManagerId == currentUserId.Value);
        foreach (var task in tasks)
        {
            labIds.Add(task.LaboratoryId);
        }

        return labIds.ToList();
    }

    /// <summary>
    /// Helper: Get all lab IDs the current user has access to (managed + department + fallback)
    /// </summary>
    private async Task<List<Guid>> GetAccessibleLaboratoryIdsAsync()
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue) return new List<Guid>();

        var labIds = new HashSet<Guid>();

        // 1. Managed labs (QLKT)
        var managed = await GetManagedLaboratoryIdsAsync();
        foreach (var id in managed) labIds.Add(id);

        // 2. From user's department
        if (!labIds.Any())
        {
            var currentUser = await _userRepository.FindAsync(currentUserId.Value);
            var departmentId = currentUser?.GetProperty<Guid?>("DepartmentId");
            if (departmentId.HasValue)
            {
                var labs = await _laboratoryRepository.GetListAsync(l => l.DepartmentId == departmentId.Value);
                foreach (var lab in labs) labIds.Add(lab.Id);
            }
        }

        // 3. From department supporter entries
        if (!labIds.Any())
        {
            var entries = await _departmentSupporterRepository.GetListAsync(
                ds => ds.UserId == currentUserId.Value && ds.ExpirationDate > DateTime.UtcNow);
            foreach (var e in entries) labIds.Add(e.LaboratoryId);
        }

        // 4. Fallback: any lab
        if (!labIds.Any())
        {
            var allLabs = await _laboratoryRepository.GetListAsync();
            foreach (var lab in allLabs) labIds.Add(lab.Id);
        }

        return labIds.ToList();
    }

    #endregion
}
