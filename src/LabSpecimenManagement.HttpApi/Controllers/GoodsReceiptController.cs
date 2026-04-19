using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.GoodsReceipts;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/goods-receipts")]
[Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Default)]
public class GoodsReceiptController : LabSpecimenManagementController
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public GoodsReceiptController(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<GoodsReceiptListDto>> GetListAsync([FromQuery] GetGoodsReceiptListInput input)
    {
        return _goodsReceiptAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<GoodsReceiptDto> GetAsync(Guid id)
    {
        return _goodsReceiptAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Create)]
    public virtual Task<GoodsReceiptDto> CreateAsync([FromBody] CreateUpdateGoodsReceiptDto input)
    {
        return _goodsReceiptAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Edit)]
    public virtual Task<GoodsReceiptDto> UpdateAsync(Guid id, [FromBody] CreateUpdateGoodsReceiptDto input)
    {
        return _goodsReceiptAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _goodsReceiptAppService.DeleteAsync(id);
    }

    [HttpPost("batch-delete")]
    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Delete)]
    public virtual Task<int> BatchDeleteAsync([FromBody] List<Guid> ids)
    {
        return _goodsReceiptAppService.BatchDeleteAsync(ids);
    }

    [HttpPost("{id}/approve")]
    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Approve)]
    public virtual Task<GoodsReceiptDto> ApproveAsync(Guid id)
    {
        return _goodsReceiptAppService.ApproveAsync(id);
    }

    [HttpPost("{id}/import-to-warehouse")]
    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.ImportToWarehouse)]
    public virtual Task<GoodsReceiptDto> ImportToWarehouseAsync(Guid id)
    {
        return _goodsReceiptAppService.ImportToWarehouseAsync(id);
    }

    [HttpPost("{id}/payments")]
    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.AddPayment)]
    public virtual Task<GoodsReceiptDto> AddPaymentAsync(Guid id, [FromBody] CreateGoodsReceiptPaymentDto input)
    {
        return _goodsReceiptAppService.AddPaymentAsync(id, input);
    }

    [HttpGet("recent-lots/{productId}")]
    public virtual Task<List<RecentLotDto>> GetRecentLotNumbersAsync(Guid productId)
    {
        return _goodsReceiptAppService.GetRecentLotNumbersAsync(productId);
    }
}
