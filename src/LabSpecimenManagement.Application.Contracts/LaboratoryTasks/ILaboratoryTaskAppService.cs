using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Users;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Application service interface for Laboratory Task
/// </summary>
public interface ILaboratoryTaskAppService : IApplicationService
{
    /// <summary>
    /// Get a single laboratory task by ID
    /// </summary>
    Task<LaboratoryTaskDto> GetAsync(Guid id);

    /// <summary>
    /// Get filtered and paged list of laboratory tasks
    /// </summary>
    Task<PagedResultDto<LaboratoryTaskDto>> GetFilteredListAsync(GetLaboratoryTaskListDto input);

    /// <summary>
    /// Update laboratory task (status, test date, notes)
    /// </summary>
    Task<LaboratoryTaskDto> UpdateAsync(Guid id, UpdateLaboratoryTaskDto input);

    /// <summary>
    /// Get tasks for current user (if user is Technical Manager of any laboratory)
    /// </summary>
    Task<ListResultDto<LaboratoryTaskDto>> GetMyLaboratoryTasksAsync();

    /// <summary>
    /// Get tasks by laboratory ID
    /// </summary>
    Task<ListResultDto<LaboratoryTaskDto>> GetByLaboratoryAsync(Guid laboratoryId);

    /// <summary>
    /// Update status (workflow)
    /// </summary>
    Task<LaboratoryTaskDto> UpdateStatusAsync(Guid id, LaboratoryTaskStatus newStatus);

    /// <summary>
    /// Set test date
    /// </summary>
    Task<LaboratoryTaskDto> SetTestDateAsync(Guid id, DateTime testDate);

    /// <summary>
    /// Delete laboratory task
    /// </summary>
    Task DeleteAsync(Guid id);

    #region Phase 6 - Workflow Methods

    /// <summary>
    /// Gán phương pháp và kỹ thuật viên cho các chỉ tiêu của một mẫu
    /// </summary>
    Task<LaboratoryTaskDto> AssignSampleIndicatorsAsync(Guid taskId, Guid sampleId, List<AssignIndicatorDto> assignments);

    /// <summary>
    /// Cập nhật kết quả cho một chỉ tiêu
    /// </summary>
    Task<LaboratoryTaskIndicatorDto> UpdateIndicatorResultAsync(Guid indicatorId, UpdateIndicatorResultDto input);

    /// <summary>
    /// Phê duyệt hoặc từ chối kết quả chỉ tiêu
    /// </summary>
    Task<LaboratoryTaskIndicatorDto> ApproveIndicatorAsync(Guid indicatorId, ApproveIndicatorDto input);

    /// <summary>
    /// Gửi kết quả (chuyển status sang ResultSent)
    /// </summary>
    Task<LaboratoryTaskDto> SendResultsAsync(Guid taskId);

    /// <summary>
    /// Lấy danh sách phương pháp theo chỉ tiêu
    /// </summary>
    Task<ListResultDto<MethodLookupDto>> GetMethodsByIndicatorAsync(Guid indicatorId);

    /// <summary>
    /// Lấy danh sách kỹ thuật viên theo khoa phòng
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetTechniciansByLaboratoryAsync(Guid laboratoryId);

    #endregion
}
