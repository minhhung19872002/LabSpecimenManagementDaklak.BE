using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Subcontractors;

public class SubcontractorDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public string? TaxCode { get; set; }
    public bool IsActive { get; set; }
    public string? Capabilities { get; set; }
    public decimal? EvaluationScore { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? Notes { get; set; }
    public List<SubcontractorCertificationDto>? Certifications { get; set; }
}

public class SubcontractorCertificationDto : EntityDto<Guid>
{
    public Guid SubcontractorId { get; set; }
    public string Name { get; set; }
    public string IssuedBy { get; set; }
    public DateTime IssuedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? FilePath { get; set; }
    public string Status => ExpiryDate < DateTime.Now ? "expired" :
                            ExpiryDate < DateTime.Now.AddMonths(3) ? "expiring" : "valid";
}
