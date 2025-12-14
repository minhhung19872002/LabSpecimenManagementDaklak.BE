using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateCustomerDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? TaxCode { get; set; }

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}
