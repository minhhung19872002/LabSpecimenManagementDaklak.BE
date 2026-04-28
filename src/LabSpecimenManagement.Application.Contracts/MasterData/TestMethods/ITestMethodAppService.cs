using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.MaterialConsumption;
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
    Task<ListResultDto<TestMethodDto>> GetAllAsync();

    // Methods with indicators
    Task<List<TestMethodWithIndicatorsDto>> GetAllWithIndicatorsAsync();
    Task<TestMethodWithIndicatorsDto> GetWithIndicatorsAsync(Guid id);

    // Indicator management
    Task<List<TestMethodIndicatorDto>> GetIndicatorsAsync(Guid testMethodId);
    Task AddIndicatorsAsync(Guid testMethodId, AddIndicatorsToTestMethodDto input);
    Task<TestMethodIndicatorDto> UpdateIndicatorAsync(Guid testMethodId, Guid indicatorId, Guid? specimenTypeId, UpdateTestMethodIndicatorDto input);
    Task RemoveIndicatorAsync(Guid testMethodId, Guid indicatorId, Guid? specimenTypeId = null);
    Task ToggleIndicatorActiveAsync(Guid testMethodId, Guid indicatorId, Guid? specimenTypeId);

    // Toggle active status
    Task<TestMethodDto> ToggleActiveAsync(Guid id);

    // Batch remove indicators - uses composite key (IndicatorId + SpecimenTypeId)
    Task<int> RemoveIndicatorsBatchAsync(Guid testMethodId, List<IndicatorKeyDto> indicatorKeys);

    // Material consumption
    Task<List<MaterialConsumptionItemDto>> GetConsumptionAsync(Guid testMethodIndicatorId);
    Task SaveConsumptionAsync(Guid testMethodIndicatorId, SaveMaterialConsumptionDto input);
    Task<List<ConsumptionStatusDto>> CheckConsumptionStatusAsync(CheckConsumptionStatusInput input);
}
