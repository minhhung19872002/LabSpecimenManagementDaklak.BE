using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.PublishedReports;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/test-requests")]
[Authorize(LabSpecimenManagementPermissions.TestRequests.Default)]
public class TestRequestController : LabSpecimenManagementController
{
    private readonly ITestRequestAppService _testRequestAppService;

    public TestRequestController(ITestRequestAppService testRequestAppService)
    {
        _testRequestAppService = testRequestAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestRequestDto>> GetListAsync([FromQuery] GetTestRequestListDto input)
    {
        return await _testRequestAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<TestRequestDto> GetAsync(Guid id)
    {
        return _testRequestAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Create)]
    public virtual async Task<TestRequestDto> CreateAsync([FromBody] CreateTestRequestDto input)
    {
        return await _testRequestAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual async Task<TestRequestDto> UpdateAsync(Guid id, [FromBody] UpdateTestRequestDto input)
    {
        return await _testRequestAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testRequestAppService.DeleteAsync(id);
    }

    /// <summary>
    /// Generate next contract number with format HDYYYYMMDD###
    /// </summary>
    [HttpGet("generate-contract-number")]
    public Task<string> GenerateContractNumberAsync()
    {
        return _testRequestAppService.GenerateContractNumberAsync();
    }

    /// <summary>
    /// Print test request form - update status from New to Pending
    /// </summary>
    [HttpPost("{id}/print")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task PrintAsync(Guid id)
    {
        return _testRequestAppService.PrintTestRequestAsync(id);
    }

    /// <summary>
    /// Update status to PendingApproval after printing result form
    /// </summary>
    [HttpPost("{id}/pending-approval")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task UpdateToPendingApprovalAsync(Guid id)
    {
        return _testRequestAppService.UpdateToPendingApprovalAsync(id);
    }

    /// <summary>
    /// Approve test request - update status to Approved
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task ApproveAsync(Guid id)
    {
        return _testRequestAppService.ApproveAsync(id);
    }

    /// <summary>
    /// Reject result - QLCL rejects result, update status back to InProgress
    /// Trả lại cho QLKT xem xét
    /// </summary>
    [HttpPost("{id}/reject-result")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task RejectResultAsync(Guid id, [FromBody] RejectWithReasonDto input)
    {
        return _testRequestAppService.RejectResultAsync(id, input.Reason);
    }

    /// <summary>
    /// Send result to customer - update status to ResultSent
    /// </summary>
    [HttpPost("{id}/send-result")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task SendResultAsync(Guid id)
    {
        return _testRequestAppService.SendResultAsync(id);
    }

    /// <summary>
    /// Complete test request - update status to Completed
    /// </summary>
    [HttpPost("{id}/complete")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task CompleteAsync(Guid id)
    {
        return _testRequestAppService.CompleteAsync(id);
    }

    /// <summary>
    /// Complete test request with postal delivery image
    /// Used when DeliveryPostal = true
    /// </summary>
    [HttpPost("{id}/complete-with-image")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task CompleteWithImageAsync(Guid id, [FromBody] CompleteWithImageDto input)
    {
        return _testRequestAppService.CompleteWithImageAsync(id, input);
    }

    /// <summary>
    /// Send sample - update status from Pending to SampleSent
    /// </summary>
    [HttpPost("{id}/send-sample")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task SendSampleAsync(Guid id)
    {
        return _testRequestAppService.SendSampleAsync(id);
    }

    /// <summary>
    /// Approve changes - QLCL approves changes, update status back to previous status
    /// </summary>
    [HttpPost("{id}/approve-changes")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task ApproveChangesAsync(Guid id)
    {
        return _testRequestAppService.ApproveChangesAsync(id);
    }

    /// <summary>
    /// Reject changes - QLCL rejects changes, rollback data and update status back to previous status
    /// </summary>
    [HttpPost("{id}/reject-changes")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task RejectChangesAsync(Guid id, [FromBody] RejectWithReasonDto input)
    {
        return _testRequestAppService.RejectChangesAsync(id, input.Reason);
    }

    /// <summary>
    /// Get change history for a test request
    /// </summary>
    [HttpGet("{id}/change-history")]
    public virtual Task<System.Collections.Generic.List<TestRequestChangeHistoryDto>> GetChangeHistoryAsync(Guid id)
    {
        return _testRequestAppService.GetChangeHistoryAsync(id);
    }

    /// <summary>
    /// Get timeline/progress for a test request
    /// </summary>
    [HttpGet("{id}/timeline")]
    public virtual Task<TestRequestTimelineDto> GetTimelineAsync(Guid id)
    {
        return _testRequestAppService.GetTimelineAsync(id);
    }

    /// <summary>
    /// Get pending tasks for SampleReceiver (used for popup reminder)
    /// </summary>
    [HttpGet("sample-receiver-pending-tasks")]
    public virtual Task<List<SampleReceiverPendingTaskDto>> GetSampleReceiverPendingTasksAsync()
    {
        return _testRequestAppService.GetSampleReceiverPendingTasksAsync();
    }

    /// <summary>
    /// Get approved indicators for printing (In KQ)
    /// </summary>
    [HttpGet("{id}/approved-indicators")]
    public virtual Task<List<QualityManagementIndicatorDto>> GetApprovedIndicatorsAsync(Guid id)
    {
        return _testRequestAppService.GetApprovedIndicatorsAsync(id);
    }

    /// <summary>
    /// Print single indicator result (In KQ Đơn)
    /// </summary>
    [HttpPost("{id}/print-single")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task<PrintResultOutputDto> PrintSingleResultAsync(Guid id, [FromBody] PrintSingleDto input)
    {
        return _testRequestAppService.PrintSingleResultAsync(id, input.IndicatorId, input.Note, input.TemplateName);
    }

    /// <summary>
    /// Print mixed indicators result (In KQ Hỗn hợp)
    /// </summary>
    [HttpPost("{id}/print-mixed")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task<PrintResultOutputDto> PrintMixedResultAsync(Guid id, [FromBody] PrintMixedDto input)
    {
        return _testRequestAppService.PrintMixedResultAsync(id, input.IndicatorIds, input.Note, input.TemplateName);
    }

    /// <summary>
    /// Gửi kế toán - Tạo Receipt tự động
    /// </summary>
    [HttpPost("{id}/send-to-accounting")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task<Guid> SendToAccountingAsync(Guid id)
    {
        return _testRequestAppService.SendToAccountingAsync(id);
    }

    /// <summary>
    /// Gửi QLKT - Chuyển từ Paid sang UnderReview (Đang xem xét YC)
    /// </summary>
    [HttpPost("{id}/send-to-qlkt")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task SendToQLKTAsync(Guid id)
    {
        return _testRequestAppService.SendToQLKTAsync(id);
    }

    /// <summary>
    /// Cập nhật LaboratoryId và RequestDate cho các chỉ tiêu trước khi gửi QLKT
    /// </summary>
    [HttpPost("{id}/update-indicators-laboratory")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task UpdateIndicatorsLaboratoryAsync(Guid id, [FromBody] UpdateIndicatorsLaboratoryInput input)
    {
        return _testRequestAppService.UpdateIndicatorsLaboratoryAsync(id, input);
    }

    /// <summary>
    /// Hủy yêu cầu thử nghiệm
    /// </summary>
    [HttpPost("{id}/cancel-request")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task CancelRequestAsync(Guid id, [FromBody] CancelTestRequestDto input)
    {
        return _testRequestAppService.CancelRequestAsync(id, input);
    }

    #region Workflow Redesign - BPGN Confirmation (Per-Indicator)

    /// <summary>
    /// Lấy danh sách chỉ tiêu chờ KH xác nhận
    /// </summary>
    [HttpGet("{id}/indicators-pending-confirmation")]
    public virtual Task<List<IndicatorPendingConfirmationDto>> GetIndicatorsPendingConfirmationAsync(Guid id)
    {
        return _testRequestAppService.GetIndicatorsPendingConfirmationAsync(id);
    }

    /// <summary>
    /// BPGN xác nhận từng chỉ tiêu (KH xác nhận đồng ý/không đồng ý)
    /// </summary>
    [HttpPost("{id}/indicators/{indicatorId}/confirm")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task<IndicatorPendingConfirmationDto> ConfirmIndicatorAsync(
        Guid id,
        Guid indicatorId,
        [FromBody] ConfirmIndicatorDto input)
    {
        return _testRequestAppService.ConfirmIndicatorAsync(id, indicatorId, input);
    }

    /// <summary>
    /// BPGN xác nhận nhiều chỉ tiêu cùng lúc (KH xác nhận đồng ý/không đồng ý)
    /// </summary>
    [HttpPost("{id}/bulk-confirm-indicators")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task<List<IndicatorPendingConfirmationDto>> BulkConfirmIndicatorsAsync(
        Guid id,
        [FromBody] BulkConfirmIndicatorsDto input)
    {
        return _testRequestAppService.BulkConfirmIndicatorsAsync(id, input);
    }

    #endregion

    #region BPGN - Confirm Delivery to Customer

    /// <summary>
    /// BPGN xác nhận đã bàn giao kết quả cho khách hàng
    /// Status tự động tính dựa trên chỉ tiêu đã xuất bên QLCL
    /// </summary>
    [HttpPost("{id}/confirm-delivery")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task ConfirmDeliveryToCustomerAsync(
        Guid id,
        [FromBody] ConfirmDeliveryInput? input = null)
    {
        return _testRequestAppService.ConfirmDeliveryToCustomerAsync(id, input);
    }

    /// <summary>
    /// Lấy danh sách báo cáo đã xuất của TestRequest (để xem lịch sử)
    /// </summary>
    [HttpGet("{id}/published-reports")]
    public virtual Task<List<PublishedReportDto>> GetPublishedReportsAsync(Guid id)
    {
        return _testRequestAppService.GetPublishedReportsAsync(id);
    }

    #endregion

    #region Cost Change History

    /// <summary>
    /// Lấy lịch sử thay đổi tiền của phiếu yêu cầu
    /// </summary>
    [HttpGet("{id}/cost-history")]
    public virtual Task<List<CostChangeHistoryDto>> GetCostChangeHistoryAsync(Guid id)
    {
        return _testRequestAppService.GetCostChangeHistoryAsync(id);
    }

    #endregion
}
