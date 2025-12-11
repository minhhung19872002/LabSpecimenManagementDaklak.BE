using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Tests;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Area("app")]
[Route("api/app/test")]
public class TestController : AbpController
{
    private readonly ITestAppService _testAppService;

    public TestController(ITestAppService testAppService)
    {
        _testAppService = testAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return await _testAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual async Task<TestDto> GetAsync(Guid id)
    {
        return await _testAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual async Task<TestDto> CreateAsync(CreateTestDto input)
    {
        return await _testAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual async Task<TestDto> UpdateAsync(Guid id, UpdateTestDto input)
    {
        return await _testAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public virtual async Task DeleteAsync(Guid id)
    {
        await _testAppService.DeleteAsync(id);
    }
}
