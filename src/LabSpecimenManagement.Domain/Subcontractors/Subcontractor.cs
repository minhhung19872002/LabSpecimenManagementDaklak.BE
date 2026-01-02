using System;
using System.Collections.Generic;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Subcontractors;

/// <summary>
/// Nhà thầu phụ - Subcontractor
/// ISO/IEC 17025:2017 Section 7.1.1
/// </summary>
public class Subcontractor : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã nhà thầu (auto-generated)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên nhà thầu
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Địa chỉ
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Số điện thoại
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Người liên hệ
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Mã số thuế
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Trạng thái (active/inactive)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Năng lực thử nghiệm (JSON array)
    /// </summary>
    public string? Capabilities { get; set; }

    /// <summary>
    /// Điểm đánh giá (0-5)
    /// </summary>
    public decimal? EvaluationScore { get; set; }

    /// <summary>
    /// Ngày bắt đầu hợp đồng
    /// </summary>
    public DateTime? ContractStartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc hợp đồng
    /// </summary>
    public DateTime? ContractEndDate { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Danh sách chứng nhận
    /// </summary>
    public virtual ICollection<SubcontractorCertification> Certifications { get; set; }

    protected Subcontractor()
    {
        Certifications = new List<SubcontractorCertification>();
    }

    public Subcontractor(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        Certifications = new List<SubcontractorCertification>();
    }
}
