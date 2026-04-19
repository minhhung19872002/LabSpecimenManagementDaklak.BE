using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Suppliers;

public interface ISupplierAppService : IApplicationService
{
    Task<PagedResultDto<SupplierDto>> GetListAsync(SupplierFilterDto input);
    Task<SupplierDto> GetAsync(Guid id);
    Task<SupplierDto> CreateAsync(CreateUpdateSupplierDto input);
    Task<SupplierDto> UpdateAsync(Guid id, CreateUpdateSupplierDto input);
    Task DeleteAsync(Guid id);
    Task<int> BatchDeleteAsync(List<Guid> ids);
    Task ToggleActiveAsync(Guid id);
    Task<List<string>> GetSupplierGroupsAsync();
}
