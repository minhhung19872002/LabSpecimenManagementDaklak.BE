using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Users;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Partial class for Supporter Management operations
/// </summary>
public partial class LaboratoryTaskAppService
{
    #region Supporter Management

    [Authorize]
    public async Task<List<LaboratoryTaskSupporterDto>> GetSupportersAsync(Guid taskId)
    {
        var supporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);

        // Batch fetch user names
        var userNames = await _userNameProvider.GetUserNamesAsync(supporters.Select(s => (Guid?)s.UserId));

        var dtos = supporters.Select(supporter => new LaboratoryTaskSupporterDto
        {
            Id = supporter.Id,
            LaboratoryTaskId = supporter.LaboratoryTaskId,
            UserId = supporter.UserId,
            FullName = userNames.TryGetValue(supporter.UserId, out var name) ? name : null,
            CreationTime = supporter.CreationTime
        }).ToList();

        return dtos.OrderBy(x => x.FullName).ToList();
    }

    [Authorize]
    public async Task<List<LaboratoryTaskSupporterDto>> AddSupportersAsync(Guid taskId, ManageSupportersDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only admin or QLKT can add supporters (supporter không có quyền thêm supporter)
        var currentUserId = CurrentUser.Id;
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);
        var isQlkt = task.TechnicalManagerId.HasValue && task.TechnicalManagerId == currentUserId;

        if (!isAdmin && !isQlkt)
        {
            throw new ForbiddenAccessException("Chỉ QLKT mới có quyền thêm người hỗ trợ");
        }

        // Get existing supporter user IDs
        var existingSupporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var existingUserIds = existingSupporters.Select(x => x.UserId).ToHashSet();

        foreach (var userId in input.UserIds)
        {
            // Skip if already a supporter
            if (existingUserIds.Contains(userId))
            {
                continue;
            }

            // Skip if user is the technical manager
            if (task.TechnicalManagerId == userId)
            {
                continue;
            }

            var supporter = new LaboratoryTaskSupporter(
                GuidGenerator.Create(),
                taskId,
                userId
            );
            supporter.CreatorId = CurrentUser.Id;

            await _supporterRepository.InsertAsync(supporter);
        }

        return await GetSupportersAsync(taskId);
    }

    [Authorize]
    public async Task RemoveSupporterAsync(Guid taskId, Guid supporterId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only admin or QLKT can remove supporters
        var currentUserId = CurrentUser.Id;
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);
        var isQlkt = task.TechnicalManagerId.HasValue && task.TechnicalManagerId == currentUserId;

        if (!isAdmin && !isQlkt)
        {
            throw new ForbiddenAccessException("Chỉ QLKT mới có quyền xóa người hỗ trợ");
        }

        var supporter = await _supporterRepository.FindAsync(supporterId);
        if (supporter == null || supporter.LaboratoryTaskId != taskId)
        {
            throw new EntityNotFoundException("Supporter", supporterId);
        }

        await _supporterRepository.DeleteAsync(supporter);
    }

    [Authorize]
    public async Task<ListResultDto<UserLookupDto>> GetAvailableSupportersAsync(Guid taskId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Get existing supporter user IDs
        var existingSupporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var existingUserIds = existingSupporters.Select(x => x.UserId).ToHashSet();

        // Add QLKT to excluded list
        if (task.TechnicalManagerId.HasValue)
        {
            existingUserIds.Add(task.TechnicalManagerId.Value);
        }

        // Get QLKT's department ID to filter available supporters
        Guid? qlktDepartmentId = null;
        if (task.TechnicalManagerId.HasValue)
        {
            var qlktUser = await _userRepository.FindAsync(task.TechnicalManagerId.Value);
            if (qlktUser != null)
            {
                qlktDepartmentId = qlktUser.GetProperty<Guid?>("DepartmentId");
            }
        }

        // Get all active users and exclude existing supporters and QLKT
        var users = await _userRepository.GetListAsync();
        var availableUsers = users
            .Where(u => u.IsActive && !existingUserIds.Contains(u.Id))
            // Filter by same department as QLKT (if QLKT has department)
            .Where(u => !qlktDepartmentId.HasValue || u.GetProperty<Guid?>("DepartmentId") == qlktDepartmentId.Value)
            .Select(u => new UserLookupDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Name = u.Name,
                Surname = u.Surname,
                DisplayName = !string.IsNullOrWhiteSpace(u.Name)
                    ? $"{u.Surname} {u.Name}".Trim()
                    : u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive
            })
            .OrderBy(u => u.DisplayName)
            .ToList();

        return new ListResultDto<UserLookupDto>(availableUsers);
    }

    /// <summary>
    /// Check if current user can manage task (admin, QLKT or supporter)
    /// </summary>
    private async Task<bool> CanManageTaskAsync(LaboratoryTask task)
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            return false;
        }

        // Admin can manage all tasks
        if (CurrentUser.IsInRole(AppConstants.RoleAdmin))
        {
            return true;
        }

        // QLKT can manage
        if (task.TechnicalManagerId == currentUserId.Value)
        {
            return true;
        }

        // Supporters can manage
        var supporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);
        if (supporters.Any(x => x.UserId == currentUserId.Value))
        {
            return true;
        }

        // Department supporters can manage (not expired)
        return (await _departmentSupporterRepository.GetListAsync(
            ds => ds.UserId == currentUserId.Value &&
                  ds.LaboratoryId == task.LaboratoryId &&
                  ds.ExpirationDate > DateTime.UtcNow)).Any();
    }

    /// <summary>
    /// Kiểm tra user hiện tại có phải supporter của bất kỳ task chưa hoàn thành nào không
    /// Dùng để xác định quyền truy cập sidebar "Quản lý công việc" và "Quản lý thử nghiệm"
    /// </summary>
    [Authorize]
    public async Task<bool> IsCurrentUserSupporterAsync()
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            return false;
        }

        // Check department supporter (not expired)
        var isDepartmentSupporter = (await _departmentSupporterRepository.GetListAsync(
            ds => ds.UserId == currentUserId.Value && ds.ExpirationDate > DateTime.UtcNow)).Any();
        if (isDepartmentSupporter)
        {
            return true;
        }

        // Lấy danh sách task IDs mà user là supporter
        var supporterRecords = await _supporterRepository.GetListAsync(s => s.UserId == currentUserId.Value);
        if (!supporterRecords.Any())
        {
            return false;
        }

        var taskIds = supporterRecords.Select(s => s.LaboratoryTaskId).ToList();

        // Kiểm tra xem có task nào chưa hoàn thành không (status != SentToQuality)
        var incompleteTasks = await _laboratoryTaskRepository.GetListAsync(
            t => taskIds.Contains(t.Id) && t.Status != LaboratoryTaskStatus.SentToQuality);

        return incompleteTasks.Any();
    }

    #endregion
}
