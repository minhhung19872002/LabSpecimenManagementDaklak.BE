using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/products")]
[Authorize(LabSpecimenManagementPermissions.Products.Default)]
public class ProductController : LabSpecimenManagementController
{
    private readonly IProductAppService _productAppService;

    public ProductController(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<ProductDto>> GetListAsync([FromQuery] ProductFilterDto input)
    {
        return _productAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<ProductDto> GetAsync(Guid id)
    {
        return _productAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Products.Create)]
    public virtual Task<ProductDto> CreateAsync([FromBody] CreateUpdateProductDto input)
    {
        return _productAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Products.Edit)]
    public virtual Task<ProductDto> UpdateAsync(Guid id, [FromBody] CreateUpdateProductDto input)
    {
        return _productAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Products.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _productAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<ProductDto>> GetActiveProductsAsync()
    {
        return _productAppService.GetActiveProductsAsync();
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.Products.Edit)]
    public virtual Task ToggleActiveAsync(Guid id)
    {
        return _productAppService.ToggleActiveAsync(id);
    }

    [HttpDelete("{productId}/attachments/{attachmentId}")]
    [Authorize(LabSpecimenManagementPermissions.Products.Edit)]
    public virtual Task DeleteAttachmentAsync(Guid productId, Guid attachmentId)
    {
        return _productAppService.DeleteAttachmentAsync(productId, attachmentId);
    }

    [HttpDelete("{productId}/skus/{skuId}")]
    [Authorize(LabSpecimenManagementPermissions.Products.Edit)]
    public virtual Task DeleteSkuAsync(Guid productId, Guid skuId)
    {
        return _productAppService.DeleteSkuAsync(productId, skuId);
    }

    [HttpGet("{productId}/sources")]
    public virtual Task<PagedResultDto<ProductSourceDto>> GetProductSourcesAsync(Guid productId, [FromQuery] GetProductSourcesInput input)
    {
        return _productAppService.GetProductSourcesAsync(productId, input);
    }

    [HttpGet("{id}/detail")]
    public virtual Task<ProductDetailDto> GetDetailAsync(Guid id, [FromQuery] Guid? warehouseId)
    {
        return _productAppService.GetDetailAsync(id, warehouseId);
    }

    [HttpGet("{id}/import-history")]
    public virtual Task<PagedResultDto<ProductImportHistoryDto>> GetImportHistoryAsync(Guid id, [FromQuery] GetProductImportHistoryInput input)
    {
        return _productAppService.GetImportHistoryAsync(id, input);
    }
}
