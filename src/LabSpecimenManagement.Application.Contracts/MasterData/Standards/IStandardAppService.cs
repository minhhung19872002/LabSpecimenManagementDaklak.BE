using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface IStandardAppService : IApplicationService
{
    Task<PagedResultDto<StandardDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<StandardDto> GetAsync(Guid id);
    Task<StandardDto> CreateAsync(CreateStandardDto input);
    Task<StandardDto> UpdateAsync(Guid id, UpdateStandardDto input);
    Task DeleteAsync(Guid id);
    Task<ListResultDto<StandardDto>> GetActiveAsync();

    // Toggle active status
    Task<StandardDto> ToggleActiveAsync(Guid id);

    // Standards with indicators
    Task<List<StandardWithIndicatorsDto>> GetAllWithIndicatorsAsync();
    Task<StandardWithIndicatorsDto> GetWithIndicatorsAsync(Guid id);

    // Indicator management
    Task<List<StandardIndicatorDto>> GetIndicatorsAsync(Guid standardId);
    Task AddIndicatorsAsync(Guid standardId, AddIndicatorsToStandardDto input);
    Task<StandardIndicatorDto> UpdateIndicatorAsync(Guid standardId, Guid id, UpdateStandardIndicatorDto input);
    Task RemoveIndicatorAsync(Guid standardId, Guid id);
    Task<StandardIndicatorDto> ToggleIndicatorActiveAsync(Guid standardId, Guid id);
}
