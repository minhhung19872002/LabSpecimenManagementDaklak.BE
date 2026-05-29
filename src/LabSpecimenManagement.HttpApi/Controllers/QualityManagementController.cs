using LabSpecimenManagement.Common;
using LabSpecimenManagement.QualityManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/quality-management")]
[Authorize]
public class QualityManagementController : LabSpecimenManagementController
{
    private readonly IQualityManagementAppService _qualityManagementAppService;

    public QualityManagementController(IQualityManagementAppService qualityManagementAppService)
    {
        _qualityManagementAppService = qualityManagementAppService;
    }

    /// <summary>
    /// Get filtered and paged list of Quality Management Tasks
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<QualityManagementTaskDto>> GetListAsync([FromQuery] GetQualityManagementListInput input)
    {
        return _qualityManagementAppService.GetListAsync(input);
    }

    /// <summary>
    /// Get Quality Management Task by ID
    /// </summary>
    [HttpGet("{id}")]
    public virtual Task<QualityManagementTaskDto> GetAsync(Guid id)
    {
        return _qualityManagementAppService.GetAsync(id);
    }

    /// <summary>
    /// Get Quality Management Task by ID with full indicator list across all labs
    /// </summary>
    [HttpGet("{id}/full-detail")]
    public virtual Task<QualityManagementTaskDto> GetFullDetailAsync(Guid id)
    {
        return _qualityManagementAppService.GetFullDetailAsync(id);
    }

    /// <summary>
    /// Get statistics for dashboard
    /// </summary>
    [HttpGet("statistics")]
    public virtual Task<QualityManagementStatisticsDto> GetStatisticsAsync()
    {
        return _qualityManagementAppService.GetStatisticsAsync();
    }

    /// <summary>
    /// Approve indicator (QLCL duyệt kết quả)
    /// </summary>
    [HttpPost("indicators/{indicatorId}/approve")]
    public virtual Task<QualityManagementIndicatorDto> ApproveIndicatorAsync(Guid indicatorId)
    {
        return _qualityManagementAppService.ApproveIndicatorAsync(indicatorId);
    }

    /// <summary>
    /// Reject indicator (QLCL từ chối kết quả)
    /// </summary>
    [HttpPost("indicators/{indicatorId}/reject")]
    public virtual Task<QualityManagementIndicatorDto> RejectIndicatorAsync(Guid indicatorId, [FromBody] RejectWithReasonDto input)
    {
        return _qualityManagementAppService.RejectIndicatorAsync(indicatorId, input.Reason);
    }

    /// <summary>
    /// Bulk approve indicators (QLCL duyệt nhiều chỉ tiêu cùng lúc)
    /// </summary>
    [HttpPost("indicators/bulk-approve")]
    public virtual Task<BulkApproveIndicatorsResultDto> BulkApproveIndicatorsAsync([FromBody] BulkApproveIndicatorsDto input)
    {
        return _qualityManagementAppService.BulkApproveIndicatorsAsync(input.IndicatorIds);
    }

    /// <summary>
    /// Bulk reject indicators (QLCL từ chối nhiều chỉ tiêu cùng lúc)
    /// </summary>
    [HttpPost("indicators/bulk-reject")]
    public virtual Task<BulkRejectIndicatorsResultDto> BulkRejectIndicatorsAsync([FromBody] BulkRejectIndicatorsDto input)
    {
        return _qualityManagementAppService.BulkRejectIndicatorsAsync(input.IndicatorIds, input.Reason);
    }

    /// <summary>
    /// Update indicator standard (chọn tiêu chuẩn so sánh)
    /// </summary>
    [HttpPut("indicators/{indicatorId}/standard")]
    public virtual Task<QualityManagementIndicatorDto> UpdateIndicatorStandardAsync(Guid indicatorId, [FromBody] UpdateIndicatorStandardDto input)
    {
        return _qualityManagementAppService.UpdateIndicatorStandardAsync(indicatorId, input.StandardId);
    }

    /// <summary>
    /// Get indicator status history
    /// </summary>
    [HttpGet("indicators/{indicatorId}/history")]
    public virtual Task<List<QualityIndicatorStatusHistoryDto>> GetIndicatorHistoryAsync(Guid indicatorId)
    {
        return _qualityManagementAppService.GetIndicatorHistoryAsync(indicatorId);
    }

    /// <summary>
    /// Get task history - supports both ResultApproval and ChangeApproval
    /// </summary>
    [HttpGet("{taskId}/history")]
    public virtual Task<TaskHistoryResponseDto> GetTaskHistoryAsync(Guid taskId)
    {
        return _qualityManagementAppService.GetTaskHistoryAsync(taskId);
    }

    /// <summary>
    /// Print single result (In KQ Đơn) - generates PDF and returns file name
    /// </summary>
    [HttpPost("{taskId}/print-single")]
    public virtual Task<PrintResultOutputDto> PrintSingleResultAsync(Guid taskId, [FromBody] PrintSingleDto input)
    {
        return _qualityManagementAppService.PrintSingleResultAsync(taskId, input.IndicatorId, input.Note, input.TemplateName, input.VicePresidentId);
    }

    /// <summary>
    /// Print mixed result (In KQ Hỗn hợp) - generates PDF and returns file name
    /// </summary>
    [HttpPost("{taskId}/print-mixed")]
    public virtual Task<PrintResultOutputDto> PrintMixedResultAsync(Guid taskId, [FromBody] PrintMixedDto input)
    {
        return _qualityManagementAppService.PrintMixedResultAsync(taskId, input.IndicatorIds, input.Note, input.TemplateName, input.VicePresidentId);
    }

    /// <summary>
    /// Print multiple single results (In KQ Đơn batch) - generates (labs × templates) PDFs
    /// </summary>
    [HttpPost("{taskId}/print-multi-single")]
    public virtual Task<PrintMultiResultOutputDto> PrintMultiSingleResultAsync(Guid taskId, [FromBody] PrintMultiSingleDto input)
    {
        return _qualityManagementAppService.PrintMultiSingleResultAsync(taskId, input);
    }

    /// <summary>
    /// Send result to customer (Gửi kết quả)
    /// </summary>
    [HttpPost("{taskId}/send-result")]
    public virtual Task SendResultAsync(Guid taskId)
    {
        return _qualityManagementAppService.SendResultAsync(taskId);
    }

    /// <summary>
    /// Xác nhận đã giao kết quả cho BPGN
    /// </summary>
    [HttpPost("{taskId}/confirm-handover")]
    public virtual Task ConfirmHandoverAsync(Guid taskId, [FromBody] ConfirmHandoverInput? input = null)
    {
        return _qualityManagementAppService.ConfirmHandoverAsync(taskId, input);
    }

    /// <summary>
    /// Get standards lookup for dropdown
    /// </summary>
    [HttpGet("standards-lookup")]
    public virtual Task<List<StandardLookupDto>> GetStandardsLookupAsync()
    {
        return _qualityManagementAppService.GetStandardsLookupAsync();
    }

    /// <summary>
    /// Get standard indicator info for auto-fill reference limit
    /// </summary>
    [HttpGet("standard-indicator")]
    public virtual Task<StandardIndicatorInfoDto?> GetStandardIndicatorInfoAsync(
        [FromQuery] Guid standardId,
        [FromQuery] Guid indicatorId,
        [FromQuery] Guid methodId)
    {
        return _qualityManagementAppService.GetStandardIndicatorInfoAsync(standardId, indicatorId, methodId);
    }

    /// <summary>
    /// Approve changes (Phê duyệt thay đổi phiếu yêu cầu)
    /// </summary>
    [HttpPost("{taskId}/approve-changes")]
    public virtual Task ApproveChangesAsync(Guid taskId)
    {
        return _qualityManagementAppService.ApproveChangesAsync(taskId);
    }

    /// <summary>
    /// Reject changes (Từ chối thay đổi phiếu yêu cầu)
    /// </summary>
    [HttpPost("{taskId}/reject-changes")]
    public virtual Task RejectChangesAsync(Guid taskId, [FromBody] RejectWithReasonDto input)
    {
        return _qualityManagementAppService.RejectChangesAsync(taskId, input.Reason);
    }

    #region Work Reminder

    /// <summary>
    /// Lấy danh sách công việc cần làm cho Quality Manager (QLCL) (dùng cho popup reminder)
    /// </summary>
    [HttpGet("quality-manager-pending-tasks")]
    public virtual Task<List<QualityManagerPendingTaskDto>> GetQualityManagerPendingTasksAsync()
    {
        return _qualityManagementAppService.GetQualityManagerPendingTasksAsync();
    }

    #endregion

    #region Sample Assignment Log

    /// <summary>
    /// Lấy danh sách Sổ nhận mẫu giao việc - Flat indicator list với thông tin Sample, TestRequest, QLKT, KTV
    /// </summary>
    [HttpGet("sample-assignment-log")]
    public virtual Task<PagedResultDto<SampleAssignmentLogDto>> GetSampleAssignmentLogAsync([FromQuery] GetSampleAssignmentLogInput input)
    {
        return _qualityManagementAppService.GetSampleAssignmentLogAsync(input);
    }

    /// <summary>
    /// Thống kê Sổ nhận mẫu giao việc cho QLCL theo QLCLDisplayStatus
    /// </summary>
    [HttpGet("sample-assignment-log/statistics")]
    public virtual Task<QLCLSampleAssignmentLogStatisticsDto> GetSampleAssignmentLogStatisticsAsync()
    {
        return _qualityManagementAppService.GetSampleAssignmentLogStatisticsAsync();
    }

    #endregion
}
