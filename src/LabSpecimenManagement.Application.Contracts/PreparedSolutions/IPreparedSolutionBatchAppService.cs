using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.PreparedSolutions;

public interface IPreparedSolutionBatchAppService : IApplicationService
{
    Task<PagedResultDto<PreparedSolutionBatchDto>> GetListAsync(GetPreparedSolutionBatchesInput input);
    Task<PreparedSolutionBatchDto> GetAsync(Guid id);
    Task<PreparedSolutionBatchDto> CreateAsync(CreateUpdatePreparedSolutionBatchDto input);
    Task<PreparedSolutionBatchDto> UpdateAsync(Guid id, CreateUpdatePreparedSolutionBatchDto input);
    Task<PreparedSolutionBatchDto> ProcessAsync(Guid id, ProcessPreparedSolutionDto input);

    // Helpers for dropdowns
    Task<List<PreparedSolutionBatchDto>> GetWarningItemsAsync();
    Task<List<PreparedSolutionWarehouseProductDto>> GetWarehouseProductsAsync(Guid warehouseId, int? productType);
}
