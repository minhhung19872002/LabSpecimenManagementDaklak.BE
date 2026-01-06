using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.QualityManagement;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Disable auto API generation - using custom TestRequestController instead
/// </summary>
[RemoteService(IsEnabled = false)]
public interface ITestRequestAppService :
    ICrudAppService<
        TestRequestDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTestRequestDto,
        UpdateTestRequestDto>
{
    /// <summary>
    /// Get filtered and paged list of test requests
    /// </summary>
    Task<PagedResultDto<TestRequestDto>> GetFilteredListAsync(GetTestRequestListDto input);

    /// <summary>
    /// When printing test request form - update status from New to Pending
    /// </summary>
    Task PrintTestRequestAsync(Guid id);

    /// <summary>
    /// When user sends sample - update status from Pending to SampleSent
    /// </summary>
    Task SendSampleAsync(Guid id);

    /// <summary>
    /// When QLKT assigns method + technician - update status to InProgress
    /// </summary>
    Task UpdateToInProgressAsync(Guid testRequestId);

    /// <summary>
    /// When printing test result form - update status to PendingApproval
    /// </summary>
    Task UpdateToPendingApprovalAsync(Guid id);

    /// <summary>
    /// When leader approves - update status to Approved
    /// </summary>
    Task ApproveAsync(Guid id);

    /// <summary>
    /// When sending result to customer - update status to ResultSent
    /// </summary>
    Task SendResultAsync(Guid id);

    /// <summary>
    /// When completing the test request - update status to Completed
    /// </summary>
    Task CompleteAsync(Guid id);

    /// <summary>
    /// Complete test request with postal delivery image
    /// Used when DeliveryPostal = true
    /// </summary>
    Task CompleteWithImageAsync(Guid id, CompleteWithImageDto input);

    /// <summary>
    /// QLCL phê duyệt thay đổi: PendingChangeApproval → Previous status
    /// </summary>
    Task ApproveChangesAsync(Guid id);

    /// <summary>
    /// QLCL từ chối thay đổi: Rollback + PendingChangeApproval → Previous status
    /// </summary>
    Task RejectChangesAsync(Guid id, string reason);

    /// <summary>
    /// Lấy lịch sử thay đổi của phiếu yêu cầu
    /// </summary>
    Task<List<TestRequestChangeHistoryDto>> GetChangeHistoryAsync(Guid id);

    /// <summary>
    /// QLCL từ chối kết quả: PendingApproval → InProgress
    /// Trả lại cho QLKT xem xét
    /// </summary>
    Task RejectResultAsync(Guid id, string reason);

    /// <summary>
    /// Kiểm tra số hợp đồng có tồn tại không
    /// </summary>
    /// <param name="contractNumber">Số hợp đồng cần kiểm tra</param>
    /// <param name="excludeId">ID của phiếu yêu cầu cần loại trừ (dùng khi update)</param>
    /// <returns>True nếu số HĐ đã tồn tại</returns>
    Task<bool> CheckContractNumberExistsAsync(string contractNumber, Guid? excludeId = null);

    /// <summary>
    /// Lấy thông tin tiến trình (timeline) của phiếu yêu cầu
    /// </summary>
    Task<TestRequestTimelineDto> GetTimelineAsync(Guid id);

    /// <summary>
    /// Lấy danh sách công việc cần làm cho SampleReceiver (dùng cho popup reminder)
    /// </summary>
    Task<List<SampleReceiverPendingTaskDto>> GetSampleReceiverPendingTasksAsync();

    /// <summary>
    /// Lấy danh sách chỉ tiêu đã duyệt của TestRequest (dùng cho In KQ)
    /// </summary>
    Task<List<QualityManagementIndicatorDto>> GetApprovedIndicatorsAsync(Guid id);

    /// <summary>
    /// In kết quả đơn (1 chỉ tiêu)
    /// </summary>
    Task<PrintResultOutputDto> PrintSingleResultAsync(Guid id, Guid indicatorId, string? note = null, string? templateName = null);

    /// <summary>
    /// In kết quả hỗn hợp (nhiều chỉ tiêu)
    /// </summary>
    Task<PrintResultOutputDto> PrintMixedResultAsync(Guid id, List<Guid> indicatorIds, string? note = null, string? templateName = null);

    /// <summary>
    /// Gửi kế toán - Tạo Receipt tự động và cập nhật SentToAccounting = true
    /// </summary>
    Task<Guid> SendToAccountingAsync(Guid id);

    /// <summary>
    /// Gửi QLKT - Chuyển từ Paid sang UnderReview (Đang xem xét YC)
    /// </summary>
    Task SendToQLKTAsync(Guid id);

    /// <summary>
    /// Hủy yêu cầu thử nghiệm
    /// </summary>
    /// <param name="id">ID của yêu cầu</param>
    /// <param name="input">Thông tin hủy (bao gồm lý do)</param>
    Task CancelRequestAsync(Guid id, CancelTestRequestDto input);

    /// <summary>
    /// Sinh số hợp đồng tự động với format HDYYYYMMDD###
    /// </summary>
    Task<string> GenerateContractNumberAsync();

    #region Workflow Redesign - BPGN Confirmation (Per-Indicator)

    /// <summary>
    /// Lấy danh sách chỉ tiêu chờ KH xác nhận
    /// </summary>
    Task<List<IndicatorPendingConfirmationDto>> GetIndicatorsPendingConfirmationAsync(Guid id);

    /// <summary>
    /// BPGN xác nhận từng chỉ tiêu (KH xác nhận đồng ý/không đồng ý)
    /// </summary>
    /// <param name="id">TestRequest ID</param>
    /// <param name="indicatorId">LaboratoryTaskIndicator ID</param>
    /// <param name="input">Thông tin xác nhận</param>
    Task<IndicatorPendingConfirmationDto> ConfirmIndicatorAsync(Guid id, Guid indicatorId, ConfirmIndicatorDto input);

    /// <summary>
    /// BPGN xác nhận nhiều chỉ tiêu cùng lúc (KH xác nhận đồng ý/không đồng ý)
    /// </summary>
    /// <param name="id">TestRequest ID</param>
    /// <param name="input">Thông tin xác nhận</param>
    Task<List<IndicatorPendingConfirmationDto>> BulkConfirmIndicatorsAsync(Guid id, BulkConfirmIndicatorsDto input);

    #endregion

    #region BPGN - Confirm Delivery to Customer

    /// <summary>
    /// BPGN xác nhận đã bàn giao kết quả cho khách hàng
    /// Status tự động tính dựa trên chỉ tiêu đã xuất bên QLCL:
    /// - Còn chỉ tiêu chưa xuất → PartialResultSent
    /// - Xuất hết → Completed
    /// </summary>
    /// <param name="id">TestRequest ID</param>
    /// <param name="input">Optional note when confirming</param>
    Task ConfirmDeliveryToCustomerAsync(Guid id, ConfirmDeliveryInput? input = null);

    /// <summary>
    /// Lấy danh sách báo cáo đã xuất của TestRequest (để xem lịch sử)
    /// </summary>
    Task<List<PublishedReports.PublishedReportDto>> GetPublishedReportsAsync(Guid id);

    #endregion
}
