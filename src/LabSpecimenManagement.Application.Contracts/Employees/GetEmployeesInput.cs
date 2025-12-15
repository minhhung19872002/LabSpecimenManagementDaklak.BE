using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Employees;

public class GetEmployeesInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
