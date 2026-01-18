using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Employees;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/app/employee")]
[Authorize(LabSpecimenManagementPermissions.Users.Default)]
public class EmployeeController : LabSpecimenManagementController
{
    private readonly IEmployeeAppService _employeeAppService;

    public EmployeeController(IEmployeeAppService employeeAppService)
    {
        _employeeAppService = employeeAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<EmployeeDto>> GetListAsync([FromQuery] GetEmployeesInput input)
    {
        return _employeeAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<EmployeeDto> GetAsync(Guid id)
    {
        return _employeeAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Users.Create)]
    public virtual Task<EmployeeDto> CreateAsync([FromBody] CreateEmployeeDto input)
    {
        return _employeeAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public virtual Task<EmployeeDto> UpdateAsync(Guid id, [FromBody] UpdateEmployeeDto input)
    {
        return _employeeAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Users.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _employeeAppService.DeleteAsync(id);
    }

    [HttpPut("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public virtual Task<EmployeeDto> ToggleActiveAsync(Guid id, [FromBody] ToggleActiveDto input)
    {
        return _employeeAppService.ToggleActiveAsync(id, input.IsActive);
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public virtual Task<ResetPasswordResultDto> ResetPasswordAsync(Guid id, [FromBody] ResetPasswordDto input)
    {
        return _employeeAppService.ResetPasswordAsync(id, input);
    }
}
