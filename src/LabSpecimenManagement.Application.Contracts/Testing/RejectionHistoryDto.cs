using System;
using System.Collections.Generic;
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

    /// <summary>
    /// true = KTV báo không TN được, false = QLKT/QLCL từ chối kết quả
    /// </summary>
    public bool IsCannotTest { get; set; }

    /// <summary>
    /// Vai trò người báo: "Technician" hoặc "QualityManager"
    /// </summary>
    public string? ReportedByRole { get; set; }

    /// <summary>
    /// Danh sách hình ảnh đính kèm
    /// </summary>
    public List<RejectionHistoryImageDto> Images { get; set; } = new();
}
