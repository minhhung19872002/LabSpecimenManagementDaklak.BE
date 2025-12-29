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
    Task<PrintResultOutputDto> PrintSingleResultAsync(Guid taskId, Guid indicatorId, string? note = null);

    /// <summary>
    /// Print mixed result (In KQ Hỗn hợp) - Generates PDF and updates status to PendingSignature
    /// </summary>
    Task<PrintResultOutputDto> PrintMixedResultAsync(Guid taskId, List<Guid> indicatorIds, string? note = null);

    /// <summary>
    /// Send result to customer (Gửi kết quả) - Updates status to ResultSent
    /// </summary>
    Task SendResultAsync(Guid taskId);

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
