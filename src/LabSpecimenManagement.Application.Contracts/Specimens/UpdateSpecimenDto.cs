using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Specimens;

public class UpdateSpecimenDto
{
    [Required]
    public Guid TestRequestId { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [Required]
    [StringLength(256)]
    public string Type { get; set; }

    [Required]
    public DateTime ReceivedDate { get; set; }

    [Required]
    public SpecimenStatus Status { get; set; }

    [StringLength(256)]
    public string? StorageLocation { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(2000)]
    public string? Images { get; set; }

    [StringLength(1000)]
    public string? Deviation { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(50)]
    public string Unit { get; set; }
}
