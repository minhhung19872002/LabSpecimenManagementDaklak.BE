using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.GoodsReceipts;

public interface IGoodsReceiptAppService : IApplicationService
{
    Task<PagedResultDto<GoodsReceiptListDto>> GetListAsync(GetGoodsReceiptListInput input);
    Task<GoodsReceiptDto> GetAsync(Guid id);
    Task<GoodsReceiptDto> CreateAsync(CreateUpdateGoodsReceiptDto input);
    Task<GoodsReceiptDto> UpdateAsync(Guid id, CreateUpdateGoodsReceiptDto input);
    Task DeleteAsync(Guid id);
    Task<int> BatchDeleteAsync(List<Guid> ids);
    Task<GoodsReceiptDto> ApproveAsync(Guid id);
    Task<GoodsReceiptDto> ImportToWarehouseAsync(Guid id);
    Task<GoodsReceiptDto> AddPaymentAsync(Guid id, CreateGoodsReceiptPaymentDto input);
    Task<List<RecentLotDto>> GetRecentLotNumbersAsync(Guid productId);
}
