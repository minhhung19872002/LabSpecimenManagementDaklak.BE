using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.WarehouseImports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/warehouse-import-receipts")]
[Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Default)]
public class WarehouseImportReceiptController : LabSpecimenManagementController
{
    private readonly IWarehouseImportReceiptAppService _warehouseImportReceiptAppService;

    public WarehouseImportReceiptController(IWarehouseImportReceiptAppService warehouseImportReceiptAppService)
    {
        _warehouseImportReceiptAppService = warehouseImportReceiptAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<WarehouseImportReceiptListDto>> GetListAsync([FromQuery] PagedAndSortedResultRequestDto input)
    {
        return _warehouseImportReceiptAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<WarehouseImportReceiptDto> GetAsync(Guid id)
    {
        return _warehouseImportReceiptAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Create)]
    public virtual Task<WarehouseImportReceiptDto> CreateAsync([FromBody] CreateUpdateWarehouseImportReceiptDto input)
    {
        return _warehouseImportReceiptAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Create)]
    public virtual Task<WarehouseImportReceiptDto> UpdateAsync(Guid id, [FromBody] CreateUpdateWarehouseImportReceiptDto input)
    {
        return _warehouseImportReceiptAppService.UpdateAsync(id, input);
    }

    [HttpPost("{id}/confirm")]
    [Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Confirm)]
    public virtual Task<WarehouseImportReceiptDto> ConfirmAsync(Guid id)
    {
        return _warehouseImportReceiptAppService.ConfirmAsync(id);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Create)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _warehouseImportReceiptAppService.DeleteAsync(id);
    }

    [HttpGet("next-code")]
    public virtual Task<string> GetNextCodeAsync()
    {
        return _warehouseImportReceiptAppService.GetNextCodeAsync();
    }

    [HttpGet("eligible-goods-receipts")]
    public virtual Task<List<EligibleGoodsReceiptForImportDto>> GetEligibleGoodsReceiptsAsync()
    {
        return _warehouseImportReceiptAppService.GetEligibleGoodsReceiptsAsync();
    }

    [HttpGet("importable-items/{goodsReceiptId}")]
    public virtual Task<List<GoodsReceiptItemForImportDto>> GetImportableItemsAsync(Guid goodsReceiptId)
    {
        return _warehouseImportReceiptAppService.GetImportableItemsAsync(goodsReceiptId);
    }

    [HttpGet("defective-history/{goodsReceiptId}")]
    public virtual Task<List<DefectiveHistoryItemDto>> GetDefectiveHistoryAsync(Guid goodsReceiptId)
    {
        return _warehouseImportReceiptAppService.GetDefectiveHistoryAsync(goodsReceiptId);
    }
}
