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
    /// Cập nhật tiêu chuẩn so sánh cho chỉ tiêu
    /// </summary>
    Task<LaboratoryTaskIndicatorDto> UpdateIndicatorStandardAsync(Guid indicatorId, Guid? standardId);

    /// <summary>
    /// Cập nhật ghi chú cho chỉ tiêu
    /// </summary>
    Task<LaboratoryTaskIndicatorDto> UpdateIndicatorNoteAsync(Guid indicatorId, string? note);

    /// <summary>
    /// Gửi kết quả (chuyển status sang SentToQuality)
    /// </summary>
    Task<LaboratoryTaskDto> SendResultsAsync(Guid taskId);

    /// <summary>
    /// Lấy danh sách phương pháp theo chỉ tiêu (indicatorId optional - nếu không truyền sẽ trả về tất cả)
    /// </summary>
    Task<ListResultDto<MethodLookupDto>> GetMethodsByIndicatorAsync(Guid? indicatorId);

    /// <summary>
    /// Lấy danh sách kỹ thuật viên theo khoa phòng (nếu laboratoryId null thì lấy tất cả)
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetTechniciansByLaboratoryAsync(Guid? laboratoryId);

    #endregion

    #region Supporter Management

    /// <summary>
    /// Lấy danh sách người hỗ trợ của task
    /// </summary>
    Task<List<LaboratoryTaskSupporterDto>> GetSupportersAsync(Guid taskId);

    /// <summary>
    /// Thêm người hỗ trợ vào task
    /// </summary>
    Task<List<LaboratoryTaskSupporterDto>> AddSupportersAsync(Guid taskId, ManageSupportersDto input);

    /// <summary>
    /// Xóa người hỗ trợ khỏi task
    /// </summary>
    Task RemoveSupporterAsync(Guid taskId, Guid supporterId);

    /// <summary>
    /// Lấy danh sách user trong Laboratory để chọn làm supporter
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetAvailableSupportersAsync(Guid taskId);

    #endregion

    #region Bulk Approval

    /// <summary>
    /// Bulk approve/reject indicators
    /// </summary>
    Task<List<LaboratoryTaskIndicatorDto>> BulkApproveIndicatorsAsync(Guid taskId, BulkApproveIndicatorsDto input);

    #endregion

    #region Send to Quality Management

    /// <summary>
    /// Gửi kết quả được chọn cho QLCL
    /// </summary>
    Task<LaboratoryTaskDto> SendToQualityAsync(Guid taskId, SendToQualityDto input);

    /// <summary>
    /// Lấy danh sách QLCL để chọn người nhận (filter theo cùng phòng ban với QLKT của task)
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetQualityManagersAsync(Guid taskId);

    #endregion

    #region Work Reminder

    /// <summary>
    /// Lấy danh sách công việc cần làm cho Lab Technical Manager (dùng cho popup reminder)
    /// </summary>
    Task<List<LabTechManagerPendingTaskDto>> GetLabTechManagerPendingTasksAsync();

    /// <summary>
    /// Lấy danh sách công việc cần làm cho Lab Technician (KTV) (dùng cho popup reminder)
    /// </summary>
    Task<List<LabTechnicianPendingTaskDto>> GetLabTechnicianPendingTasksAsync();

    #endregion
}
