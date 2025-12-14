using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.SpecimenHistories;

public class UpdateSpecimenHistoryDto
{
    [Required]
    [StringLength(200)]
    public string Action { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; }

    [Required]
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
}
