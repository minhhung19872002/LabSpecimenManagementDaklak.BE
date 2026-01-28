using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.NCRs;

public class GetNCRListDto : PagedAndSortedResultRequestDto
{
    public string? Search { get; set; }
    public NCRStatus? Status { get; set; }
}
