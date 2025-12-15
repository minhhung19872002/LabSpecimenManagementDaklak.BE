using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Subcontractors;

public class CreateUpdateSubcontractorDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(20)]
    public string? TaxCode { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON array of capabilities
    /// </summary>
    public string? Capabilities { get; set; }

    [Range(0, 5)]
    public decimal? EvaluationScore { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public List<CreateUpdateCertificationDto>? Certifications { get; set; }
}

public class CreateUpdateCertificationDto
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [StringLength(100)]
    public string IssuedBy { get; set; }

    [Required]
    public DateTime IssuedDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [StringLength(50)]
    public string? CertificateNumber { get; set; }

    [StringLength(512)]
    public string? FilePath { get; set; }
}
