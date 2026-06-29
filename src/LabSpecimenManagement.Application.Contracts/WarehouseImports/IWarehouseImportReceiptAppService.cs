using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.WarehouseImports;

public interface IWarehouseImportReceiptAppService : IApplicationService
{
    Task<PagedResultDto<WarehouseImportReceiptListDto>> GetListAsync(GetWarehouseImportReceiptListInput input);
    Task<WarehouseImportReceiptStatisticsDto> GetStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<WarehouseImportReceiptDto> GetAsync(Guid id);
    Task<WarehouseImportReceiptDto> CreateAsync(CreateUpdateWarehouseImportReceiptDto input);
    Task<WarehouseImportReceiptDto> UpdateAsync(Guid id, CreateUpdateWarehouseImportReceiptDto input);
    Task<WarehouseImportReceiptDto> ConfirmAsync(Guid id);
    Task DeleteAsync(Guid id);
    Task<string> GetNextCodeAsync();
    Task<List<EligibleGoodsReceiptForImportDto>> GetEligibleGoodsReceiptsAsync();
    Task<List<GoodsReceiptItemForImportDto>> GetImportableItemsAsync(Guid goodsReceiptId);
    Task<List<DefectiveHistoryItemDto>> GetDefectiveHistoryAsync(Guid goodsReceiptId);
}
