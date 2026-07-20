using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/equipments")]
[Authorize(LabSpecimenManagementPermissions.Equipments.Default)]
public class EquipmentController : LabSpecimenManagementController
{
    private readonly IEquipmentAppService _appService;

    public EquipmentController(IEquipmentAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<EquipmentListDto>> GetListAsync([FromQuery] GetEquipmentListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<EquipmentDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Equipments.Create)]
    public virtual Task<EquipmentDto> CreateAsync([FromBody] CreateUpdateEquipmentDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Equipments.Edit)]
    public virtual Task<EquipmentDto> UpdateAsync(Guid id, [FromBody] CreateUpdateEquipmentDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpPost("{id}/update-status")]
    [Authorize(LabSpecimenManagementPermissions.Equipments.UpdateStatus)]
    public virtual Task<EquipmentDto> UpdateStatusAsync(Guid id, [FromBody] UpdateEquipmentStatusDto input)
    {
        return _appService.UpdateStatusAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Equipments.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    [Authorize(LabSpecimenManagementPermissions.Equipments.Edit)]
    public virtual Task DeleteAttachmentAsync(Guid id, Guid attachmentId)
    {
        return _appService.DeleteAttachmentAsync(id, attachmentId);
    }

    [HttpGet("{id}/cost-configs")]
    public virtual Task<List<EquipmentCostConfigDto>> GetCostConfigListAsync(Guid id)
    {
        return _appService.GetCostConfigListAsync(id);
    }

    [HttpGet("{id}/cost-configs/active")]
    public virtual Task<EquipmentCostConfigDto?> GetActiveCostConfigAsync(Guid id)
    {
        return _appService.GetActiveCostConfigAsync(id);
    }

    [HttpPost("{id}/cost-configs")]
    [Authorize(LabSpecimenManagementPermissions.Equipments.Edit)]
    public virtual Task<EquipmentCostConfigDto> SaveCostConfigAsync(Guid id, [FromBody] CreateEquipmentCostConfigDto input)
    {
        return _appService.SaveCostConfigAsync(id, input);
    }
}
