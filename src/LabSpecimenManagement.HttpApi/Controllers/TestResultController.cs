using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.TestResults;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Area("app")]
[Route("api/app/test-result")]
public class TestResultController : LabSpecimenManagementController
{
    private readonly ITestResultAppService _testResultAppService;

    public TestResultController(ITestResultAppService testResultAppService)
    {
        _testResultAppService = testResultAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<TestResultDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return _testResultAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<TestResultDto> GetAsync(Guid id)
    {
        return _testResultAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual Task<TestResultDto> CreateAsync(CreateTestResultDto input)
    {
        return _testResultAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual Task<TestResultDto> UpdateAsync(Guid id, UpdateTestResultDto input)
    {
        return _testResultAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testResultAppService.DeleteAsync(id);
    }
}
