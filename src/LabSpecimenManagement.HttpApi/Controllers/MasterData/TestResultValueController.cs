using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/test-result-values")]
[Authorize]
public class TestResultValueController : LabSpecimenManagementController
{
    private readonly ITestResultValueAppService _testResultValueAppService;

    public TestResultValueController(ITestResultValueAppService testResultValueAppService)
    {
        _testResultValueAppService = testResultValueAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestResultValueDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _testResultValueAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<TestResultValueDto> GetAsync(Guid id)
    {
        return _testResultValueAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual Task<TestResultValueDto> CreateAsync([FromBody] CreateUpdateTestResultValueDto input)
    {
        return _testResultValueAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual Task<TestResultValueDto> UpdateAsync(Guid id, [FromBody] CreateUpdateTestResultValueDto input)
    {
        return _testResultValueAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testResultValueAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<TestResultValueDto>> GetActiveTestResultValuesAsync()
    {
        return _testResultValueAppService.GetActiveTestResultValuesAsync();
    }
}
