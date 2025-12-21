using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// DTO for rejection history
/// </summary>
public class RejectionHistoryDto : EntityDto<Guid>
{
    /// <summary>
    /// LaboratoryTaskIndicator ID
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Lý do từ chối
    /// </summary>
    public string RejectionReason { get; set; } = string.Empty;

    /// <summary>
    /// Ngày từ chối
    /// </summary>
    public DateTime RejectedDate { get; set; }

    /// <summary>
    /// ID người từ chối
    /// </summary>
    public Guid RejectedById { get; set; }

    /// <summary>
    /// Tên người từ chối
    /// </summary>
    public string? RejectedByName { get; set; }
}
