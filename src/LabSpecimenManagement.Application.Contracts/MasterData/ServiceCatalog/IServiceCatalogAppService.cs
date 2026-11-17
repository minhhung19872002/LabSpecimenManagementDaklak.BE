using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.ServiceCatalog;

/// <summary>
/// L2 — Dịch vụ & Bảng giá
/// </summary>
public interface IServiceCatalogAppService : IApplicationService
{
    Task<ListResultDto<ServiceCatalogItemDto>> GetListAsync(GetServiceCatalogInput input);

    Task<BulkCreateServicesResultDto> BulkCreateAsync(BulkCreateServicesInput input);

    Task<ServiceCatalogItemDto> UpdateAsync(Guid id, UpdateServiceInput input);

    Task<ListResultDto<ServicePriceVersionDto>> GetPriceVersionsAsync(Guid id);

    Task<ServiceCatalogItemDto> SetPriceAsync(Guid id, SetServicePriceInput input);

    Task DeletePriceVersionAsync(Guid versionId);

    Task<BulkUpdatePriceResultDto> BulkUpdatePriceAsync(BulkUpdatePriceInput input);

    Task<ImportServicePricesResultDto> ImportPricesAsync(ImportServicePricesInput input);

    Task ToggleActiveAsync(Guid id);

    Task DeleteAsync(Guid id);

    Task<int> DeleteManyAsync(List<Guid> ids);

    /// <summary>Áp giá các version đến ngày hiệu lực (worker + gọi tay)</summary>
    Task<int> ApplyDuePricesAsync();
}
