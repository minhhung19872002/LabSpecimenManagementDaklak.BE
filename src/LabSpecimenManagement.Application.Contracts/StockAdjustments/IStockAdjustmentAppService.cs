using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.StockAdjustments;

public interface IStockAdjustmentAppService : IApplicationService
{
    Task<PagedResultDto<StockAdjustmentListDto>> GetListAsync(GetStockAdjustmentListInput input);
    Task<StockAdjustmentDto> GetAsync(Guid id);
    Task<StockAdjustmentDto> CreateAsync(CreateUpdateStockAdjustmentDto input);
    Task<StockAdjustmentDto> UpdateAsync(Guid id, CreateUpdateStockAdjustmentDto input);
    Task DeleteAsync(Guid id);
    Task<StockAdjustmentDto> ConfirmAsync(Guid id);
    Task<ConfirmPreviewDto> GetConfirmPreviewAsync(Guid id);
    Task<List<WarehouseProductLotDto>> GetWarehouseProductLotsAsync(Guid warehouseId);
    Task<List<WarehouseWithLotsDto>> GetWarehouseWithChildrenProductLotsAsync(Guid warehouseId);
}
