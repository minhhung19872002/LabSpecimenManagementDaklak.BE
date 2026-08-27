using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace LabSpecimenManagement.Products;

/// <summary>
/// Interface cho Product AppService
/// </summary>
public interface IProductAppService : IApplicationService
{
    Task<PagedResultDto<ProductDto>> GetListAsync(ProductFilterDto input);
    Task<ProductDto> GetAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateUpdateProductDto input);
    Task<ProductDto> UpdateAsync(Guid id, CreateUpdateProductDto input);
    Task DeleteAsync(Guid id);
    Task ToggleActiveAsync(Guid id);
    Task<ListResultDto<ProductDto>> GetActiveProductsAsync();
    Task DeleteAttachmentAsync(Guid productId, Guid attachmentId);
    Task DeleteSkuAsync(Guid productId, Guid skuId);
    Task<PagedResultDto<ProductSourceDto>> GetProductSourcesAsync(Guid productId, GetProductSourcesInput input);
    Task<ProductDetailDto> GetDetailAsync(Guid id, Guid? warehouseId);
    Task<List<ProductLotStockDto>> GetLotStocksAsync(Guid id, Guid? warehouseId);
    Task<PagedResultDto<ProductImportHistoryDto>> GetImportHistoryAsync(Guid id, GetProductImportHistoryInput input);
    Task<PagedResultDto<ProductMaterialReceiptHistoryDto>> GetMaterialReceiptHistoryAsync(
        Guid id, GetProductMaterialReceiptHistoryInput input);
    Task<PagedResultDto<ProductUsageLogItemDto>> GetUsageLogAsync(Guid id, GetProductUsageLogInput input);
    Task<List<BarcodeLotGroupDto>> GetBarcodeLotHistoryAsync(Guid id, Guid? warehouseId);
    Task<IRemoteStreamContent> ExportInventoryAsync(ProductFilterDto input);
    Task<ExportProductAppendixOutput> ExportProductAppendixAsync(Guid warehouseId);
}
