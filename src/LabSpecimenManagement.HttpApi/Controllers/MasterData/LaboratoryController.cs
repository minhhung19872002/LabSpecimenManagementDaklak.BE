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
[Route("api/v1/laboratories")]
[Authorize(LabSpecimenManagementPermissions.Laboratories.Default)]
public class LaboratoryController : LabSpecimenManagementController
{
    private readonly ILaboratoryAppService _laboratoryAppService;

    public LaboratoryController(ILaboratoryAppService laboratoryAppService)
    {
        _laboratoryAppService = laboratoryAppService;
    }

    [HttpGet]
    public virtual Task<ListResultDto<LaboratoryDto>> GetAllAsync()
    {
        return _laboratoryAppService.GetAllAsync();
    }

    [HttpGet("{id}")]
    public virtual Task<LaboratoryDto> GetAsync(Guid id)
    {
        return _laboratoryAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Laboratories.Create)]
    public virtual Task<LaboratoryDto> CreateAsync([FromBody] CreateUpdateLaboratoryDto input)
    {
        return _laboratoryAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Laboratories.Edit)]
    public virtual Task<LaboratoryDto> UpdateAsync(Guid id, [FromBody] CreateUpdateLaboratoryDto input)
    {
        return _laboratoryAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Laboratories.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _laboratoryAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<LaboratoryDto>> GetActiveLaboratoriesAsync()
    {
        return _laboratoryAppService.GetActiveLaboratoriesAsync();
    }
}
