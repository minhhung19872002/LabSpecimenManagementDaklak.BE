using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// Input for getting paginated role list
/// </summary>
public class GetRolesInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
