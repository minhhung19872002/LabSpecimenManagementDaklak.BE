using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/suppliers-management")]
[Authorize(LabSpecimenManagementPermissions.Suppliers.Default)]
public class SupplierController : LabSpecimenManagementController
{
    private readonly ISupplierAppService _supplierAppService;

    public SupplierController(ISupplierAppService supplierAppService)
    {
        _supplierAppService = supplierAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<SupplierDto>> GetListAsync([FromQuery] SupplierFilterDto input)
    {
        return _supplierAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<SupplierDto> GetAsync(Guid id)
    {
        return _supplierAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Suppliers.Create)]
    public virtual Task<SupplierDto> CreateAsync([FromBody] CreateUpdateSupplierDto input)
    {
        return _supplierAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Suppliers.Edit)]
    public virtual Task<SupplierDto> UpdateAsync(Guid id, [FromBody] CreateUpdateSupplierDto input)
    {
        return _supplierAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Suppliers.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _supplierAppService.DeleteAsync(id);
    }

    [HttpPost("batch-delete")]
    [Authorize(LabSpecimenManagementPermissions.Suppliers.Delete)]
    public virtual Task<int> BatchDeleteAsync([FromBody] List<Guid> ids)
    {
        return _supplierAppService.BatchDeleteAsync(ids);
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.Suppliers.Edit)]
    public virtual Task ToggleActiveAsync(Guid id)
    {
        return _supplierAppService.ToggleActiveAsync(id);
    }

    [HttpGet("groups")]
    public virtual Task<List<string>> GetSupplierGroupsAsync()
    {
        return _supplierAppService.GetSupplierGroupsAsync();
    }
}
