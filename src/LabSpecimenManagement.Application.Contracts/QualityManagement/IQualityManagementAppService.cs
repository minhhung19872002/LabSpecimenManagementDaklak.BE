using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Application Service Interface for Quality Management
/// </summary>
public interface IQualityManagementAppService : IApplicationService
{
    /// <summary>
    /// Get paged list of Quality Management Tasks
    /// </summary>
    Task<PagedResultDto<QualityManagementTaskDto>> GetListAsync(GetQualityManagementListInput input);

    /// <summary>
    /// Get Quality Management Task by ID with indicators
    /// </summary>
    Task<QualityManagementTaskDto> GetAsync(Guid id);

    /// <summary>
    /// Get Quality Management Task detail with full indicator list across all labs
    /// </summary>
    Task<QualityManagementTaskDto> GetFullDetailAsync(Guid id);

    /// <summary>
    /// Get statistics for dashboard
    /// </summary>
    Task<QualityManagementStatisticsDto> GetStatisticsAsync();

    /// <summary>
    /// Approve indicator (QLCL duyệt kết quả)
    /// </summary>
    Task<QualityManagementIndicatorDto> ApproveIndicatorAsync(Guid indicatorId);

    /// <summary>
    /// Reject indicator (QLCL từ chối kết quả)
    /// </summary>
    Task<QualityManagementIndicatorDto> RejectIndicatorAsync(Guid indicatorId, string reason);

    /// <summary>
    /// Bulk approve indicators (QLCL duyệt nhiều chỉ tiêu cùng lúc)
    /// </summary>
    Task<BulkApproveIndicatorsResultDto> BulkApproveIndicatorsAsync(List<Guid> indicatorIds);

    /// <summary>
    /// Bulk reject indicators (QLCL từ chối nhiều chỉ tiêu cùng lúc)
    /// </summary>
    Task<BulkRejectIndicatorsResultDto> BulkRejectIndicatorsAsync(List<Guid> indicatorIds, string reason);

    /// <summary>
    /// Update indicator standard (chọn tiêu chuẩn so sánh)
    /// </summary>
    Task<QualityManagementIndicatorDto> UpdateIndicatorStandardAsync(Guid indicatorId, Guid? standardId);

    /// <summary>
    /// Get indicator status history
    /// </summary>
    Task<List<QualityIndicatorStatusHistoryDto>> GetIndicatorHistoryAsync(Guid indicatorId);

    /// <summary>
    /// Get task history - supports both ResultApproval and ChangeApproval
    /// </summary>
    Task<TaskHistoryResponseDto> GetTaskHistoryAsync(Guid taskId);

    /// <summary>
    /// Print single result (In KQ Đơn) - Generates PDF and updates status to PendingSignature
    /// </summary>
    Task<PrintResultOutputDto> PrintSingleResultAsync(Guid taskId, Guid indicatorId, string? note = null, string? templateName = null, Guid? vicePresidentId = null);

    /// <summary>
    /// Print mixed result (In KQ Hỗn hợp) - Generates PDF and updates status to PendingSignature
    /// </summary>
    Task<PrintResultOutputDto> PrintMixedResultAsync(Guid taskId, PrintMixedDto input);

    /// <summary>
    /// Print mixed result per sample (mỗi mẫu = 1 file riêng)
    /// </summary>
    Task<PrintMultiResultOutputDto> PrintMixedPerSampleResultAsync(Guid taskId, PrintMixedDto input);

    /// <summary>
    /// Lấy cấu hình ghi chú từ lần in hỗn hợp gần nhất của task
    /// </summary>
    Task<PrintNoteConfigDto?> GetLastPrintNoteConfigAsync(Guid taskId);

    /// <summary>
    /// Print multiple single results (In KQ Đơn batch) - Generates (labs × templates) PDFs
    /// </summary>
    Task<PrintMultiResultOutputDto> PrintMultiSingleResultAsync(Guid taskId, PrintMultiSingleDto input);

    /// <summary>
    /// Send result to customer (Gửi kết quả) - Updates status to ResultSent
    /// [DEPRECATED] Sử dụng ConfirmHandoverAsync thay thế
    /// </summary>
    Task SendResultAsync(Guid taskId);

    /// <summary>
    /// Xác nhận đã giao kết quả cho BPGN - Cập nhật status thành HandedOver
    /// Khi tất cả quality tasks đã HandedOver → TestRequest chuyển sang ReportPublished
    /// </summary>
    Task ConfirmHandoverAsync(Guid taskId, ConfirmHandoverInput? input);

    /// <summary>
    /// Get standards lookup for dropdown
    /// </summary>
    Task<List<StandardLookupDto>> GetStandardsLookupAsync();

    /// <summary>
    /// Get standard indicator info for auto-fill reference limit
    /// </summary>
    Task<StandardIndicatorInfoDto?> GetStandardIndicatorInfoAsync(Guid standardId, Guid indicatorId, Guid methodId);

    /// <summary>
    /// Approve changes (Phê duyệt thay đổi phiếu yêu cầu)
    /// </summary>
    Task ApproveChangesAsync(Guid taskId);

    /// <summary>
    /// Reject changes (Từ chối thay đổi phiếu yêu cầu)
    /// </summary>
    Task RejectChangesAsync(Guid taskId, string reason);

    #region Work Reminder

    /// <summary>
    /// Lấy danh sách công việc cần làm cho Quality Manager (QLCL) (dùng cho popup reminder)
    /// </summary>
    Task<List<QualityManagerPendingTaskDto>> GetQualityManagerPendingTasksAsync();

    #endregion

    #region Sample Assignment Log

    /// <summary>
    /// Lấy danh sách Sổ nhận mẫu giao việc - Flat indicator list với thông tin Sample, TestRequest, QLKT, KTV
    /// </summary>
    Task<PagedResultDto<SampleAssignmentLogDto>> GetSampleAssignmentLogAsync(GetSampleAssignmentLogInput input);

    /// <summary>
    /// Thống kê Sổ nhận mẫu giao việc cho QLCL theo QLCLDisplayStatus
    /// </summary>
    Task<QLCLSampleAssignmentLogStatisticsDto> GetSampleAssignmentLogStatisticsAsync();

    #endregion
}

/// <summary>
/// Standard lookup DTO for dropdown
/// </summary>
public class StandardLookupDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

/// <summary>
/// Standard Indicator info DTO for auto-fill
/// </summary>
public class StandardIndicatorInfoDto
{
    public Guid StandardId { get; set; }
    public Guid IndicatorId { get; set; }
    public Guid MethodId { get; set; }
    public string? Unit { get; set; }
    public string? ComparisonOperator { get; set; }
    public string? AllowedLimit { get; set; }
    public string? ReferenceLimit { get; set; }
}

/// <summary>
/// Result DTO for bulk approve indicators
/// </summary>
public class BulkApproveIndicatorsResultDto
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> FailedIndicators { get; set; } = new();
    public List<QualityManagementIndicatorDto> ApprovedIndicators { get; set; } = new();
}

/// <summary>
/// Result DTO for bulk reject indicators
/// </summary>
public class BulkRejectIndicatorsResultDto
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> FailedIndicators { get; set; } = new();
    public List<QualityManagementIndicatorDto> RejectedIndicators { get; set; } = new();
}

/// <summary>
/// Input DTO for confirm handover to BPGN
/// </summary>
public class ConfirmHandoverInput
{
    /// <summary>
    /// Ghi chú khi giao BPGN
    /// </summary>
    public string? Note { get; set; }
}
