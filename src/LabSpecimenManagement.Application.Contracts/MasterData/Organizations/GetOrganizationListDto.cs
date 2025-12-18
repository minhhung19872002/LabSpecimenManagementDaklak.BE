using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.Organizations;

public class GetOrganizationListDto : PagedAndSortedResultRequestDto
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
