using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Subcontractors;

/// <summary>
/// Chứng nhận của nhà thầu phụ
/// </summary>
public class SubcontractorCertification : FullAuditedEntity<Guid>
{
    /// <summary>
    /// ID nhà thầu phụ
    /// </summary>
    public Guid SubcontractorId { get; set; }

    /// <summary>
    /// Tên chứng nhận (ISO/IEC 17025, VILAS...)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Cơ quan cấp
    /// </summary>
    public string IssuedBy { get; set; }

    /// <summary>
    /// Ngày cấp
    /// </summary>
    public DateTime IssuedDate { get; set; }

    /// <summary>
    /// Ngày hết hạn
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Số chứng nhận
    /// </summary>
    public string? CertificateNumber { get; set; }

    /// <summary>
    /// Đường dẫn file scan
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual Subcontractor Subcontractor { get; set; }

    protected SubcontractorCertification()
    {
    }

    public SubcontractorCertification(
        Guid id,
        Guid subcontractorId,
        string name,
        string issuedBy,
        DateTime issuedDate,
        DateTime expiryDate
    ) : base(id)
    {
        SubcontractorId = subcontractorId;
        Name = name;
        IssuedBy = issuedBy;
        IssuedDate = issuedDate;
        ExpiryDate = expiryDate;
    }
}
