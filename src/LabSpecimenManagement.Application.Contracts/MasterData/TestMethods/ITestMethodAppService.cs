using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface ITestMethodAppService : IApplicationService
{
    Task<PagedResultDto<TestMethodDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<TestMethodDto> GetAsync(Guid id);
    Task<TestMethodDto> CreateAsync(CreateTestMethodDto input);
    Task<TestMethodDto> UpdateAsync(Guid id, UpdateTestMethodDto input);
    Task DeleteAsync(Guid id);
    Task<ListResultDto<TestMethodDto>> GetActiveAsync();

    // Methods with indicators
    Task<List<TestMethodWithIndicatorsDto>> GetAllWithIndicatorsAsync();
    Task<TestMethodWithIndicatorsDto> GetWithIndicatorsAsync(Guid id);

    // Indicator management
    Task<List<TestMethodIndicatorDto>> GetIndicatorsAsync(Guid testMethodId);
    Task AddIndicatorsAsync(Guid testMethodId, AddIndicatorsToTestMethodDto input);
    Task RemoveIndicatorAsync(Guid testMethodId, Guid indicatorId);

    // Toggle active status
    Task<TestMethodDto> ToggleActiveAsync(Guid id);
}
