using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Users;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using QualityManagement = LabSpecimenManagement.QualityManagement;

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
    /// Gán phương pháp và Thử nghiệm viên cho các chỉ tiêu của một mẫu
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
    /// Cập nhật đơn vị tính cho chỉ tiêu (không cập nhật ngược master data)
    /// </summary>
    Task<LaboratoryTaskIndicatorDto> UpdateIndicatorUnitAsync(Guid indicatorId, string? unit);

    /// <summary>
    /// Cập nhật ghi chú cho chỉ tiêu
    /// </summary>
    Task<LaboratoryTaskIndicatorDto> UpdateIndicatorNoteAsync(Guid indicatorId, string? note);

    /// <summary>
    /// Cập nhật ghi chú cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    Task BulkUpdateIndicatorNoteAsync(Guid taskId, BulkUpdateNoteDto input);

    /// <summary>
    /// Gửi kết quả (chuyển status sang SentToQuality)
    /// </summary>
    Task<LaboratoryTaskDto> SendResultsAsync(Guid taskId);

    /// <summary>
    /// Lấy danh sách phương pháp theo chỉ tiêu (indicatorId optional - nếu không truyền sẽ trả về tất cả)
    /// </summary>
    Task<ListResultDto<MethodLookupDto>> GetMethodsByIndicatorAsync(Guid? indicatorId);

    /// <summary>
    /// Lấy danh sách Thử nghiệm viên theo khoa phòng (nếu laboratoryId null thì lấy tất cả)
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetTechniciansByLaboratoryAsync(Guid? laboratoryId);

    /// <summary>
    /// Lấy danh sách Thử nghiệm viên theo phòng ban của QLKT (role LabTechnician + cùng DepartmentId với QLKT của task)
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetTechniciansByTaskAsync(Guid taskId);

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

    /// <summary>
    /// Kiểm tra user hiện tại có phải supporter của bất kỳ task chưa hoàn thành nào không
    /// Dùng để xác định quyền truy cập sidebar "Quản lý công việc" và "Quản lý thử nghiệm"
    /// </summary>
    Task<bool> IsCurrentUserSupporterAsync();

    #endregion

    #region Department Supporter Management

    /// <summary>
    /// Lấy danh sách người hỗ trợ khoa phòng
    /// </summary>
    Task<List<LaboratoryDepartmentSupporterDto>> GetDepartmentSupportersAsync();

    /// <summary>
    /// Thêm người hỗ trợ khoa phòng (có thời hạn)
    /// </summary>
    Task<List<LaboratoryDepartmentSupporterDto>> AddDepartmentSupportersAsync(AddDepartmentSupportersDto input);

    /// <summary>
    /// Xóa người hỗ trợ khoa phòng
    /// </summary>
    Task RemoveDepartmentSupporterAsync(Guid supporterId);

    /// <summary>
    /// Xóa người hỗ trợ khoa phòng đã hết hạn (gọi bởi cronjob)
    /// </summary>
    Task<int> RemoveExpiredDepartmentSupportersAsync();

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

    #region Workflow Redesign - QLKT Evaluation (Per-Indicator)

    /// <summary>
    /// QLKT đánh giá từng chỉ tiêu (Accept/RequestMethodChange/Reject)
    /// </summary>
    /// <param name="indicatorId">LaboratoryTaskIndicator ID</param>
    /// <param name="input">Thông tin đánh giá</param>
    /// <returns>DTO chỉ tiêu sau khi đánh giá</returns>
    Task<LaboratoryTaskIndicatorDto> EvaluateIndicatorAsync(Guid indicatorId, EvaluateIndicatorDto input);

    /// <summary>
    /// QLKT yêu cầu đổi phương pháp cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    /// <param name="taskId">LaboratoryTask ID</param>
    /// <param name="input">Danh sách chỉ tiêu và phương pháp mới</param>
    /// <returns>Danh sách DTO chỉ tiêu sau khi xử lý</returns>
    Task<List<LaboratoryTaskIndicatorDto>> RequestMethodChangeAsync(Guid taskId, RequestMethodChangeDto input);

    /// <summary>
    /// QLKT từ chối thử nghiệm cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    /// <param name="taskId">LaboratoryTask ID</param>
    /// <param name="input">Danh sách chỉ tiêu và lý do từ chối</param>
    /// <returns>Danh sách DTO chỉ tiêu sau khi xử lý</returns>
    Task<List<LaboratoryTaskIndicatorDto>> RejectIndicatorsForTestingAsync(Guid taskId, RejectIndicatorsForTestingDto input);

    /// <summary>
    /// QLKT duyệt tất cả chỉ tiêu còn lại (status = Pending) của task
    /// </summary>
    /// <param name="taskId">LaboratoryTask ID</param>
    /// <returns>DTO task sau khi xử lý</returns>
    Task<LaboratoryTaskDto> ApproveAllRemainingIndicatorsAsync(Guid taskId);

    #endregion

    #region Shared Sample (Mẫu chung)

    /// <summary>
    /// Kiểm tra xem có cần ẩn nút từ chối cho task này không
    /// Task của Lab Hóa trong mẫu chung không được từ chối chỉ tiêu
    /// </summary>
    /// <param name="taskId">LaboratoryTask ID</param>
    /// <returns>true nếu cần ẩn nút từ chối, false nếu không</returns>
    Task<bool> ShouldHideRejectButtonAsync(Guid taskId);

    /// <summary>
    /// Lab Vi Sinh gửi mẫu chung cho Lab Hóa
    /// </summary>
    /// <param name="id">LaboratoryTask ID của Lab Vi Sinh</param>
    /// <param name="input">Thông tin gửi mẫu (ghi chú tùy chọn)</param>
    /// <returns>DTO task sau khi gửi</returns>
    Task<LaboratoryTaskDto> SendSharedSampleAsync(Guid id, SendSharedSampleDto input);

    /// <summary>
    /// Lab Hóa xác nhận nhận mẫu chung từ Lab Vi Sinh
    /// </summary>
    /// <param name="id">LaboratoryTask ID của Lab Hóa</param>
    /// <param name="input">Thông tin nhận mẫu (ghi chú tùy chọn)</param>
    /// <returns>DTO task sau khi nhận</returns>
    Task<LaboratoryTaskDto> ReceiveSharedSampleAsync(Guid id, ReceiveSharedSampleDto input);

    #endregion

    #region Task Status Statistics

    /// <summary>
    /// Lấy thống kê số lượng công việc theo trạng thái (không filter)
    /// </summary>
    Task<TaskStatusStatisticsDto> GetTaskStatusStatisticsAsync();

    #endregion

    #region Sample Assignment Log (Sổ nhận mẫu giao việc)

    /// <summary>
    /// Lấy danh sách Sổ nhận mẫu giao việc cho QLKT - hiển thị tất cả công việc của Lab mà user quản lý
    /// </summary>
    /// <param name="input">Filter và pagination</param>
    /// <returns>Danh sách SampleAssignmentLogDto</returns>
    Task<PagedResultDto<QualityManagement.SampleAssignmentLogDto>> GetSampleAssignmentLogAsync(QualityManagement.GetSampleAssignmentLogInput input);

    #endregion
}
