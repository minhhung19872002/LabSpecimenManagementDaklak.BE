using System;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.SpecimenHistories;

public class SpecimenHistoryDto : CreationAuditedEntityDto<Guid>
{
    public Guid SpecimenId { get; set; }

    public string Action { get; set; }

    public string Location { get; set; }

    public SpecimenStatus Status { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Nhiệt độ (°C)
    /// </summary>
    public decimal? Temperature { get; set; }

    /// <summary>
    /// Độ ẩm (%)
    /// </summary>
    public decimal? Humidity { get; set; }

    /// <summary>
    /// Thời gian thực hiện
    /// </summary>
    public DateTime? Timestamp { get; set; }

    public Guid? PerformedById { get; set; }

    // Navigation properties
    public string? SpecimenCode { get; set; }

    public string? PerformedByName { get; set; }
}
