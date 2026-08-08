using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.MaterialReceipts;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[Route("api/v1/material-receipts")]
[ApiController]
[Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Default)]
public class MaterialReceiptController : LabSpecimenManagementController
{
    private readonly IMaterialReceiptAppService _materialReceiptAppService;

    public MaterialReceiptController(IMaterialReceiptAppService materialReceiptAppService)
    {
        _materialReceiptAppService = materialReceiptAppService;
    }

    [HttpGet]
    public async Task<PagedResultDto<MaterialReceiptListDto>> GetListAsync(
        [FromQuery] GetMaterialReceiptListInput input)
    {
        return await _materialReceiptAppService.GetListAsync(input);
    }

    [HttpGet("statistics")]
    public async Task<MaterialReceiptStatisticsDto> GetStatisticsAsync()
    {
        return await _materialReceiptAppService.GetStatisticsAsync();
    }

    [HttpGet("{id}")]
    public async Task<MaterialReceiptDto> GetAsync(Guid id)
    {
        return await _materialReceiptAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Create)]
    public async Task<MaterialReceiptDto> CreateAsync(
        [FromBody] CreateUpdateMaterialReceiptDto input)
    {
        return await _materialReceiptAppService.CreateAsync(input);
    }

    [HttpPost("create-and-confirm")]
    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Create)]
    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Confirm)]
    public async Task<MaterialReceiptDto> CreateAndConfirmAsync(
        [FromBody] CreateUpdateMaterialReceiptDto input)
    {
        return await _materialReceiptAppService.CreateAndConfirmAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Create)]
    public async Task<MaterialReceiptDto> UpdateAsync(
        Guid id, [FromBody] CreateUpdateMaterialReceiptDto input)
    {
        return await _materialReceiptAppService.UpdateAsync(id, input);
    }

    [HttpPost("{id}/confirm")]
    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Confirm)]
    public async Task<MaterialReceiptDto> ConfirmAsync(Guid id)
    {
        return await _materialReceiptAppService.ConfirmAsync(id);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _materialReceiptAppService.DeleteAsync(id);
    }

    [HttpGet("next-code")]
    public async Task<string> GetNextCodeAsync()
    {
        return await _materialReceiptAppService.GetNextCodeAsync();
    }

    [HttpGet("central-products")]
    public async Task<List<CentralWarehouseProductDto>> GetCentralWarehouseProductsAsync(
        [FromQuery] string? search = null, [FromQuery] int? productType = null)
    {
        return await _materialReceiptAppService.GetCentralWarehouseProductsAsync(search, productType);
    }

    [HttpGet("warehouse-stock")]
    public async Task<ReceivingWarehouseStockDto> GetReceivingWarehouseStockAsync(
        [FromQuery] Guid productId, [FromQuery] Guid warehouseId)
    {
        return await _materialReceiptAppService.GetReceivingWarehouseStockAsync(
            productId, warehouseId);
    }
}
