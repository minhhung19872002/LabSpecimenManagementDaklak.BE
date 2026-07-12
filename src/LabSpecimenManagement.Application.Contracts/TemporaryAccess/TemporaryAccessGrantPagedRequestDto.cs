using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TemporaryAccess;

public class TemporaryAccessGrantPagedRequestDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public GrantStatus? Status { get; set; }
}
