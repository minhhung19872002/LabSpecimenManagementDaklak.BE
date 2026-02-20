using LabSpecimenManagement.LaboratoryTasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/department-supporters")]
[Authorize]
public class DepartmentSupporterController : LabSpecimenManagementController
{
    private readonly ILaboratoryTaskAppService _laboratoryTaskAppService;

    public DepartmentSupporterController(ILaboratoryTaskAppService laboratoryTaskAppService)
    {
        _laboratoryTaskAppService = laboratoryTaskAppService;
    }

    [HttpGet]
    public virtual Task<List<LaboratoryDepartmentSupporterDto>> GetDepartmentSupportersAsync()
    {
        return _laboratoryTaskAppService.GetDepartmentSupportersAsync();
    }

    [HttpPost]
    public virtual Task<List<LaboratoryDepartmentSupporterDto>> AddDepartmentSupportersAsync(
        [FromBody] AddDepartmentSupportersDto input)
    {
        return _laboratoryTaskAppService.AddDepartmentSupportersAsync(input);
    }

    [HttpDelete("{supporterId}")]
    public virtual Task RemoveDepartmentSupporterAsync(Guid supporterId)
    {
        return _laboratoryTaskAppService.RemoveDepartmentSupporterAsync(supporterId);
    }
}
