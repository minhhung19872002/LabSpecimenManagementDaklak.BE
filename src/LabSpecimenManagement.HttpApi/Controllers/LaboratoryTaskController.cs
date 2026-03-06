using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/laboratory-tasks")]
[Authorize]
public class LaboratoryTaskController : LabSpecimenManagementController
{
    private readonly ILaboratoryTaskAppService _laboratoryTaskAppService;

    public LaboratoryTaskController(ILaboratoryTaskAppService laboratoryTaskAppService)
    {
        _laboratoryTaskAppService = laboratoryTaskAppService;
    }

    /// <summary>
    /// Get filtered and paged list of laboratory tasks
    /// </summary>
    [HttpGet]
    public virtual async Task<PagedResultDto<LaboratoryTaskDto>> GetListAsync([FromQuery] GetLaboratoryTaskListDto input)
    {
        return await _laboratoryTaskAppService.GetFilteredListAsync(input);
    }

    /// <summary>
    /// Get a single laboratory task by ID
    /// </summary>
    [HttpGet("{id}")]
    public virtual Task<LaboratoryTaskDto> GetAsync(Guid id)
    {
        return _laboratoryTaskAppService.GetAsync(id);
    }

    /// <summary>
    /// Update laboratory task (TestDate, Status, Notes)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> UpdateAsync(Guid id, [FromBody] UpdateLaboratoryTaskDto input)
    {
        return _laboratoryTaskAppService.UpdateAsync(id, input);
    }

    /// <summary>
    /// Update laboratory task status (workflow)
    /// </summary>
    [HttpPost("{id}/status")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> UpdateStatusAsync(Guid id, [FromBody] LaboratoryTaskStatus newStatus)
    {
        return _laboratoryTaskAppService.UpdateStatusAsync(id, newStatus);
    }

    /// <summary>
    /// Set test date for laboratory task
    /// </summary>
    [HttpPost("{id}/test-date")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> SetTestDateAsync(Guid id, [FromBody] DateTime testDate)
    {
        return _laboratoryTaskAppService.SetTestDateAsync(id, testDate);
    }

    /// <summary>
    /// Get tasks for current user (if user is Technical Manager of any laboratory)
    /// </summary>
    [HttpGet("my-tasks")]
    public virtual Task<ListResultDto<LaboratoryTaskDto>> GetMyLaboratoryTasksAsync()
    {
        return _laboratoryTaskAppService.GetMyLaboratoryTasksAsync();
    }

    /// <summary>
    /// Get tasks by laboratory ID
    /// </summary>
    [HttpGet("by-laboratory/{laboratoryId}")]
    [Authorize]
    public virtual Task<ListResultDto<LaboratoryTaskDto>> GetByLaboratoryAsync(Guid laboratoryId)
    {
        return _laboratoryTaskAppService.GetByLaboratoryAsync(laboratoryId);
    }

    /// <summary>
    /// Delete laboratory task
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public virtual Task DeleteAsync(Guid id)
    {
        return _laboratoryTaskAppService.DeleteAsync(id);
    }

    #region Phase 6 - Workflow Endpoints

    /// <summary>
    /// Gán phương pháp và Thử nghiệm viên cho các chỉ tiêu của một mẫu
    /// </summary>
    [HttpPost("{taskId}/samples/{sampleId}/assign")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> AssignSampleIndicatorsAsync(
        Guid taskId,
        Guid sampleId,
        [FromBody] List<AssignIndicatorDto> assignments)
    {
        return _laboratoryTaskAppService.AssignSampleIndicatorsAsync(taskId, sampleId, assignments);
    }

    /// <summary>
    /// Cập nhật kết quả cho một chỉ tiêu
    /// </summary>
    [HttpPut("indicators/{indicatorId}/result")]
    [Authorize]
    public virtual Task<LaboratoryTaskIndicatorDto> UpdateIndicatorResultAsync(
        Guid indicatorId,
        [FromBody] UpdateIndicatorResultDto input)
    {
        return _laboratoryTaskAppService.UpdateIndicatorResultAsync(indicatorId, input);
    }

    /// <summary>
    /// Phê duyệt hoặc từ chối kết quả chỉ tiêu
    /// </summary>
    [HttpPost("indicators/{indicatorId}/approve")]
    [Authorize]
    public virtual Task<LaboratoryTaskIndicatorDto> ApproveIndicatorAsync(
        Guid indicatorId,
        [FromBody] ApproveIndicatorDto input)
    {
        return _laboratoryTaskAppService.ApproveIndicatorAsync(indicatorId, input);
    }

    /// <summary>
    /// Cập nhật tiêu chuẩn so sánh cho chỉ tiêu
    /// </summary>
    [HttpPut("indicators/{indicatorId}/standard")]
    [Authorize]
    public virtual Task<LaboratoryTaskIndicatorDto> UpdateIndicatorStandardAsync(
        Guid indicatorId,
        [FromBody] Guid? standardId)
    {
        return _laboratoryTaskAppService.UpdateIndicatorStandardAsync(indicatorId, standardId);
    }

    /// <summary>
    /// Cập nhật đơn vị tính cho chỉ tiêu (không cập nhật ngược master data)
    /// </summary>
    [HttpPut("indicators/{indicatorId}/unit")]
    [Authorize]
    public virtual Task<LaboratoryTaskIndicatorDto> UpdateIndicatorUnitAsync(
        Guid indicatorId,
        [FromBody] string? unit)
    {
        return _laboratoryTaskAppService.UpdateIndicatorUnitAsync(indicatorId, unit);
    }

    /// <summary>
    /// Cập nhật ghi chú cho chỉ tiêu
    /// </summary>
    [HttpPut("indicators/{indicatorId}/note")]
    [Authorize]
    public virtual Task<LaboratoryTaskIndicatorDto> UpdateIndicatorNoteAsync(
        Guid indicatorId,
        [FromBody] string? note)
    {
        return _laboratoryTaskAppService.UpdateIndicatorNoteAsync(indicatorId, note);
    }

    /// <summary>
    /// Cập nhật ghi chú cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    [HttpPut("{taskId}/indicators/bulk-note")]
    [Authorize]
    public virtual Task BulkUpdateIndicatorNoteAsync(
        Guid taskId,
        [FromBody] BulkUpdateNoteDto input)
    {
        return _laboratoryTaskAppService.BulkUpdateIndicatorNoteAsync(taskId, input);
    }

    /// <summary>
    /// Gửi kết quả (chuyển status sang SentToQuality) - old method, sends all approved indicators
    /// </summary>
    [HttpPost("{taskId}/send-results")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> SendResultsAsync(Guid taskId)
    {
        return _laboratoryTaskAppService.SendResultsAsync(taskId);
    }

    /// <summary>
    /// Lấy danh sách phương pháp theo chỉ tiêu (indicatorId optional - nếu không truyền sẽ trả về tất cả)
    /// </summary>
    [HttpGet("methods-lookup")]
    public virtual Task<ListResultDto<MethodLookupDto>> GetMethodsByIndicatorAsync([FromQuery] Guid? indicatorId)
    {
        return _laboratoryTaskAppService.GetMethodsByIndicatorAsync(indicatorId);
    }

    /// <summary>
    /// Lấy danh sách Thử nghiệm viên theo khoa phòng
    /// </summary>
    [HttpGet("technicians-lookup")]
    public virtual Task<ListResultDto<UserLookupDto>> GetTechniciansByLaboratoryAsync([FromQuery] Guid? laboratoryId)
    {
        return _laboratoryTaskAppService.GetTechniciansByLaboratoryAsync(laboratoryId);
    }

    /// <summary>
    /// Lấy danh sách Thử nghiệm viên theo phòng ban của QLKT (role LabTechnician + cùng DepartmentId với QLKT của task)
    /// </summary>
    [HttpGet("{taskId}/technicians")]
    public virtual Task<ListResultDto<UserLookupDto>> GetTechniciansByTaskAsync(Guid taskId)
    {
        return _laboratoryTaskAppService.GetTechniciansByTaskAsync(taskId);
    }

    #endregion

    #region Supporter Management

    /// <summary>
    /// Get supporters for a task
    /// </summary>
    [HttpGet("{taskId}/supporters")]
    public virtual Task<List<LaboratoryTaskSupporterDto>> GetSupportersAsync(Guid taskId)
    {
        return _laboratoryTaskAppService.GetSupportersAsync(taskId);
    }

    /// <summary>
    /// Add supporters to a task
    /// </summary>
    [HttpPost("{taskId}/supporters")]
    [Authorize]
    public virtual Task<List<LaboratoryTaskSupporterDto>> AddSupportersAsync(
        Guid taskId,
        [FromBody] ManageSupportersDto input)
    {
        return _laboratoryTaskAppService.AddSupportersAsync(taskId, input);
    }

    /// <summary>
    /// Remove a supporter from a task
    /// </summary>
    [HttpDelete("{taskId}/supporters/{supporterId}")]
    [Authorize]
    public virtual Task RemoveSupporterAsync(Guid taskId, Guid supporterId)
    {
        return _laboratoryTaskAppService.RemoveSupporterAsync(taskId, supporterId);
    }

    /// <summary>
    /// Get available users to add as supporters
    /// </summary>
    [HttpGet("{taskId}/available-supporters")]
    public virtual Task<ListResultDto<UserLookupDto>> GetAvailableSupportersAsync(Guid taskId)
    {
        return _laboratoryTaskAppService.GetAvailableSupportersAsync(taskId);
    }

    /// <summary>
    /// Check if current user is a supporter for any incomplete task
    /// Used for sidebar visibility - "Quản lý công việc" and "Quản lý thử nghiệm"
    /// </summary>
    [HttpGet("is-supporter")]
    public virtual Task<bool> IsCurrentUserSupporterAsync()
    {
        return _laboratoryTaskAppService.IsCurrentUserSupporterAsync();
    }

    #endregion

    #region Department Supporter Management

    #endregion

    #region Bulk Approval

    /// <summary>
    /// Bulk approve/reject indicators
    /// </summary>
    [HttpPost("{taskId}/bulk-approve")]
    [Authorize]
    public virtual Task<List<LaboratoryTaskIndicatorDto>> BulkApproveIndicatorsAsync(
        Guid taskId,
        [FromBody] LaboratoryTasks.BulkApproveIndicatorsDto input)
    {
        return _laboratoryTaskAppService.BulkApproveIndicatorsAsync(taskId, input);
    }

    #endregion

    #region Send to Quality Management

    /// <summary>
    /// Send selected indicators to Quality Management
    /// </summary>
    [HttpPost("{taskId}/send-to-quality")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> SendToQualityAsync(
        Guid taskId,
        [FromBody] SendToQualityDto input)
    {
        return _laboratoryTaskAppService.SendToQualityAsync(taskId, input);
    }

    /// <summary>
    /// Get list of Quality Managers (filtered by same department as QLKT of task)
    /// </summary>
    [HttpGet("{taskId}/quality-managers")]
    public virtual Task<ListResultDto<UserLookupDto>> GetQualityManagersAsync(Guid taskId)
    {
        return _laboratoryTaskAppService.GetQualityManagersAsync(taskId);
    }

    #endregion

    #region Work Reminder

    /// <summary>
    /// Lấy danh sách công việc cần làm cho Lab Technical Manager (popup reminder)
    /// </summary>
    [HttpGet("lab-tech-manager-pending-tasks")]
    public virtual Task<List<LabTechManagerPendingTaskDto>> GetLabTechManagerPendingTasksAsync()
    {
        return _laboratoryTaskAppService.GetLabTechManagerPendingTasksAsync();
    }

    /// <summary>
    /// Lấy danh sách công việc cần làm cho Lab Technician/KTV (popup reminder)
    /// </summary>
    [HttpGet("lab-technician-pending-tasks")]
    public virtual Task<List<LabTechnicianPendingTaskDto>> GetLabTechnicianPendingTasksAsync()
    {
        return _laboratoryTaskAppService.GetLabTechnicianPendingTasksAsync();
    }

    #endregion

    #region Workflow Redesign - QLKT Evaluation (Per-Indicator)

    /// <summary>
    /// QLKT đánh giá từng chỉ tiêu (Accept/RequestMethodChange/Reject)
    /// </summary>
    [HttpPost("indicators/{indicatorId}/evaluate")]
    [Authorize]
    public virtual Task<LaboratoryTaskIndicatorDto> EvaluateIndicatorAsync(
        Guid indicatorId,
        [FromBody] EvaluateIndicatorDto input)
    {
        return _laboratoryTaskAppService.EvaluateIndicatorAsync(indicatorId, input);
    }

    /// <summary>
    /// QLKT yêu cầu đổi phương pháp cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    [HttpPost("{taskId}/request-method-change")]
    [Authorize]
    public virtual Task<List<LaboratoryTaskIndicatorDto>> RequestMethodChangeAsync(
        Guid taskId,
        [FromBody] RequestMethodChangeDto input)
    {
        return _laboratoryTaskAppService.RequestMethodChangeAsync(taskId, input);
    }

    /// <summary>
    /// QLKT từ chối thử nghiệm cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    [HttpPost("{taskId}/reject-indicators")]
    [Authorize]
    public virtual Task<List<LaboratoryTaskIndicatorDto>> RejectIndicatorsForTestingAsync(
        Guid taskId,
        [FromBody] RejectIndicatorsForTestingDto input)
    {
        return _laboratoryTaskAppService.RejectIndicatorsForTestingAsync(taskId, input);
    }

    /// <summary>
    /// QLKT duyệt tất cả chỉ tiêu còn lại (status = Pending) của task
    /// </summary>
    [HttpPost("{taskId}/approve-all-remaining")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> ApproveAllRemainingIndicatorsAsync(Guid taskId)
    {
        return _laboratoryTaskAppService.ApproveAllRemainingIndicatorsAsync(taskId);
    }

    #endregion

    #region Shared Sample (Mẫu chung)

    /// <summary>
    /// Kiểm tra xem có cần ẩn nút từ chối cho task này không
    /// Task của Lab Hóa trong mẫu chung không được từ chối chỉ tiêu
    /// </summary>
    [HttpGet("{taskId}/should-hide-reject-button")]
    public virtual Task<bool> ShouldHideRejectButtonAsync(Guid taskId)
    {
        return _laboratoryTaskAppService.ShouldHideRejectButtonAsync(taskId);
    }

    /// <summary>
    /// Lab Vi Sinh gửi mẫu chung cho Lab Hóa
    /// </summary>
    [HttpPost("{id}/send-shared-sample")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> SendSharedSampleAsync(
        Guid id,
        [FromBody] SendSharedSampleDto input)
    {
        return _laboratoryTaskAppService.SendSharedSampleAsync(id, input);
    }

    /// <summary>
    /// Lab Hóa xác nhận nhận mẫu chung từ Lab Vi Sinh
    /// </summary>
    [HttpPost("{id}/receive-shared-sample")]
    [Authorize]
    public virtual Task<LaboratoryTaskDto> ReceiveSharedSampleAsync(
        Guid id,
        [FromBody] ReceiveSharedSampleDto input)
    {
        return _laboratoryTaskAppService.ReceiveSharedSampleAsync(id, input);
    }

    #endregion

    #region Task Status Statistics

    /// <summary>
    /// Lấy thống kê số lượng công việc theo trạng thái (không filter)
    /// </summary>
    [HttpGet("task-status-statistics")]
    public virtual Task<TaskStatusStatisticsDto> GetTaskStatusStatisticsAsync()
    {
        return _laboratoryTaskAppService.GetTaskStatusStatisticsAsync();
    }

    #endregion

    #region Sample Assignment Log (Sổ nhận mẫu giao việc)

    /// <summary>
    /// Lấy danh sách Sổ nhận mẫu giao việc cho QLKT - hiển thị tất cả công việc của Lab mà user quản lý
    /// </summary>
    [HttpGet("sample-assignment-log")]
    public virtual async Task<PagedResultDto<SampleAssignmentLogDto>> GetSampleAssignmentLogAsync([FromQuery] GetSampleAssignmentLogInput input)
    {
        return await _laboratoryTaskAppService.GetSampleAssignmentLogAsync(input);
    }

    #endregion
}
