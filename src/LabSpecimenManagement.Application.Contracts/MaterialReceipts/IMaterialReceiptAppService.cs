using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MaterialReceipts;

public interface IMaterialReceiptAppService : IApplicationService
{
    Task<PagedResultDto<MaterialReceiptListDto>> GetListAsync(GetMaterialReceiptListInput input);
    Task<MaterialReceiptStatisticsDto> GetStatisticsAsync();
    Task<MaterialReceiptDto> GetAsync(Guid id);
    Task<MaterialReceiptDto> CreateAsync(CreateUpdateMaterialReceiptDto input);
    Task<MaterialReceiptDto> CreateAndConfirmAsync(CreateUpdateMaterialReceiptDto input);
    Task<MaterialReceiptDto> UpdateAsync(Guid id, CreateUpdateMaterialReceiptDto input);
    Task<MaterialReceiptDto> ConfirmAsync(Guid id);
    Task DeleteAsync(Guid id);
    Task<string> GetNextCodeAsync();
    Task<List<CentralWarehouseProductDto>> GetCentralWarehouseProductsAsync(
        string? search = null, int? productType = null);
    Task<ReceivingWarehouseStockDto> GetReceivingWarehouseStockAsync(
        Guid productId, Guid warehouseId);
}
