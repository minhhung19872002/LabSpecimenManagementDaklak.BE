using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/departments")]
[Authorize(LabSpecimenManagementPermissions.Departments.Default)]
public class DepartmentController : LabSpecimenManagementController
{
    private readonly IDepartmentAppService _departmentAppService;

    public DepartmentController(IDepartmentAppService departmentAppService)
    {
        _departmentAppService = departmentAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<DepartmentDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _departmentAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<DepartmentDto> GetAsync(Guid id)
    {
        return _departmentAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Departments.Create)]
    public virtual Task<DepartmentDto> CreateAsync([FromBody] CreateUpdateDepartmentDto input)
    {
        return _departmentAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Departments.Edit)]
    public virtual Task<DepartmentDto> UpdateAsync(Guid id, [FromBody] CreateUpdateDepartmentDto input)
    {
        return _departmentAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Departments.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _departmentAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<DepartmentDto>> GetActiveDepartmentsAsync()
    {
        return _departmentAppService.GetActiveDepartmentsAsync();
    }
}
