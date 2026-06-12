using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.TestBatches;

public interface ITestBatchAppService : IApplicationService
{
    Task<PagedResultDto<TestBatchListDto>> GetListAsync(GetTestBatchListInput input);

    Task<TestBatchDto> GetAsync(Guid id);

    Task<TestBatchDto> CreateAsync(CreateTestBatchDto input);

    Task DeleteAsync(Guid id);

    Task<TestBatchDto> AddItemsAsync(Guid id, AddItemsDto input);

    Task<TestBatchDto> RemoveItemsAsync(Guid id, RemoveItemsDto input);

    Task<TestBatchDto> SaveResultsAsync(Guid id, SaveTestBatchResultsDto input);

    Task<TestBatchDto> SubmitAsync(Guid id);

    Task<TestBatchDto> SubmitItemsAsync(Guid id, SubmitItemsDto input);

    Task<TestBatchDto> CalculateConsumptionAsync(Guid id);

    Task<TestBatchDto> ConfirmConsumptionAsync(Guid id, ConfirmConsumptionDto input);

    Task<TestBatchDto> CancelAsync(Guid id);

    Task<ConsumptionPreviewDto> GetConsumptionPreviewAsync(Guid id);

    Task<TestBatchDto> SubmitWithConsumptionAsync(Guid id, SubmitWithConsumptionDto input);

    Task<PagedResultDto<AvailableIndicatorDto>> GetAvailableIndicatorsAsync(GetAvailableIndicatorsInput input);
}

public class AddItemsDto
{
    public System.Collections.Generic.List<Guid> LaboratoryTaskIndicatorIds { get; set; } = new();
}

public class RemoveItemsDto
{
    public System.Collections.Generic.List<Guid> ItemIds { get; set; } = new();
}

public class SubmitItemsDto
{
    public System.Collections.Generic.List<Guid> ItemIds { get; set; } = new();
}
