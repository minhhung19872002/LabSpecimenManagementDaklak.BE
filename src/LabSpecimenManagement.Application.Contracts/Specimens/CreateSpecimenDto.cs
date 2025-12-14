using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Specimens;

public class CreateSpecimenDto
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
    public DateTime ReceivedDate { get; set; } = DateTime.Now;

    [StringLength(256)]
    public string? StorageLocation { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(2000)]
    public string? Images { get; set; }

    [StringLength(1000)]
    public string? Deviation { get; set; }

    public decimal Quantity { get; set; } = 1;

    [StringLength(50)]
    public string? Unit { get; set; } = "Cái";
}
