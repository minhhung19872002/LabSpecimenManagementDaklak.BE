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

    Task<ListResultDto<ServiceGroupDto>> GetServiceGroupsAsync();

    Task<ServiceGroupDto> CreateServiceGroupAsync(CreateServiceGroupInput input);

    Task<ServiceGroupDto> UpdateServiceGroupAsync(Guid id, CreateServiceGroupInput input);

    /// <summary>Xóa tên dịch vụ — các dòng đang thuộc nhóm chuyển về "Chưa thuộc dịch vụ". Trả về số dòng bị chuyển.</summary>
    Task<int> DeleteServiceGroupAsync(Guid id);

    /// <summary>Chuyển nhóm dịch vụ / đổi HĐ phụ cho nhiều dòng. Trả về số dòng thay đổi.</summary>
    Task<int> AssignServicesAsync(AssignServicesInput input);

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
