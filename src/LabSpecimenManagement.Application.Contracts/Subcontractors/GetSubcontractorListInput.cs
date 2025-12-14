using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Subcontractors;

public class GetSubcontractorListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Filter by code, name, or contact person
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Filter by active status (true = active, false = inactive, null = all)
    /// </summary>
    public bool? IsActive { get; set; }
}
