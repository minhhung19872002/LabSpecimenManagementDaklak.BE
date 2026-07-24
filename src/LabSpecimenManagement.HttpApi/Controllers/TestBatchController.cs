using System;
using System.Threading.Tasks;
using LabSpecimenManagement.TestBatches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/test-batches")]
[Authorize]
public class TestBatchController : LabSpecimenManagementController
{
    private readonly ITestBatchAppService _testBatchAppService;

    public TestBatchController(ITestBatchAppService testBatchAppService)
    {
        _testBatchAppService = testBatchAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<TestBatchListDto>> GetListAsync([FromQuery] GetTestBatchListInput input)
    {
        return _testBatchAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public Task<TestBatchDto> GetAsync(Guid id)
    {
        return _testBatchAppService.GetAsync(id);
    }

    [HttpPost]
    public Task<TestBatchDto> CreateAsync([FromBody] CreateTestBatchDto input)
    {
        return _testBatchAppService.CreateAsync(input);
    }

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id)
    {
        return _testBatchAppService.DeleteAsync(id);
    }

    [HttpPost("{id}/add-items")]
    public Task<TestBatchDto> AddItemsAsync(Guid id, [FromBody] AddItemsDto input)
    {
        return _testBatchAppService.AddItemsAsync(id, input);
    }

    [HttpPost("{id}/remove-items")]
    public Task<TestBatchDto> RemoveItemsAsync(Guid id, [FromBody] RemoveItemsDto input)
    {
        return _testBatchAppService.RemoveItemsAsync(id, input);
    }

    [HttpPost("{id}/save-results")]
    public Task<TestBatchDto> SaveResultsAsync(Guid id, [FromBody] SaveTestBatchResultsDto input)
    {
        return _testBatchAppService.SaveResultsAsync(id, input);
    }

    [HttpPost("{id}/submit")]
    public Task<TestBatchDto> SubmitAsync(Guid id)
    {
        return _testBatchAppService.SubmitAsync(id);
    }

    [HttpPost("{id}/submit-items")]
    public Task<TestBatchDto> SubmitItemsAsync(Guid id, [FromBody] SubmitItemsDto input)
    {
        return _testBatchAppService.SubmitItemsAsync(id, input);
    }

    [HttpPost("{id}/calculate-consumption")]
    public Task<TestBatchDto> CalculateConsumptionAsync(Guid id)
    {
        return _testBatchAppService.CalculateConsumptionAsync(id);
    }

    [HttpPost("{id}/confirm-consumption")]
    public Task<TestBatchDto> ConfirmConsumptionAsync(Guid id, [FromBody] ConfirmConsumptionDto input)
    {
        return _testBatchAppService.ConfirmConsumptionAsync(id, input);
    }

    [HttpGet("{id}/consumption-preview")]
    public Task<ConsumptionPreviewDto> GetConsumptionPreviewAsync(Guid id)
    {
        return _testBatchAppService.GetConsumptionPreviewAsync(id);
    }

    [HttpPost("{id}/submit-with-consumption")]
    public Task<TestBatchDto> SubmitWithConsumptionAsync(Guid id, [FromBody] SubmitWithConsumptionDto input)
    {
        return _testBatchAppService.SubmitWithConsumptionAsync(id, input);
    }

    [HttpPost("{id}/save-consumption-draft")]
    public Task<TestBatchDto> SaveConsumptionDraftAsync(Guid id, [FromBody] SaveConsumptionDraftDto input)
    {
        return _testBatchAppService.SaveConsumptionDraftAsync(id, input);
    }

    [HttpPost("{id}/cancel")]
    public Task<TestBatchDto> CancelAsync(Guid id)
    {
        return _testBatchAppService.CancelAsync(id);
    }

    [HttpGet("available-indicators")]
    public Task<PagedResultDto<AvailableIndicatorDto>> GetAvailableIndicatorsAsync([FromQuery] GetAvailableIndicatorsInput input)
    {
        return _testBatchAppService.GetAvailableIndicatorsAsync(input);
    }
}
