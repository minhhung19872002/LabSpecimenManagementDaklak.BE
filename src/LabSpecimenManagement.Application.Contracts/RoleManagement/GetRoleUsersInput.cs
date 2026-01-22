using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// Input for getting paginated users in a role
/// </summary>
public class GetRoleUsersInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
