using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Lịch sử đường đi của mẫu - Specimen History
/// ISO/IEC 17025:2017 - Section 7.11 Control of data and information management
/// </summary>
public class SpecimenHistory : CreationAuditedEntity<Guid>
{
    public Guid SpecimenId { get; set; }

    /// <summary>
    /// Hành động - Action performed
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// Vị trí - Location
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Trạng thái - Status at this point
    /// </summary>
    public SpecimenStatus Status { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Nhiệt độ (°C) - Temperature
    /// </summary>
    public decimal? Temperature { get; set; }

    /// <summary>
    /// Độ ẩm (%) - Humidity
    /// </summary>
    public decimal? Humidity { get; set; }

    /// <summary>
    /// Thời gian thực hiện - Timestamp
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Người thực hiện - Performed by
    /// </summary>
    public Guid? PerformedById { get; set; }

    public virtual Specimen Specimen { get; set; }

    protected SpecimenHistory()
    {
    }

    public SpecimenHistory(
        Guid id,
        Guid specimenId,
        string action,
        string location,
        SpecimenStatus status
    ) : base(id)
    {
        SpecimenId = specimenId;
        Action = action;
        Location = location;
        Status = status;
    }
}
