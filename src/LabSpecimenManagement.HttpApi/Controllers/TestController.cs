using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Tests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/tests")]
[Authorize(LabSpecimenManagementPermissions.Tests.Default)]
public class TestController : LabSpecimenManagementController
{
    private readonly ITestAppService _testAppService;

    public TestController(ITestAppService testAppService)
    {
        _testAppService = testAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestDto>> GetListAsync([FromQuery] GetTestListDto input)
    {
        return await _testAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual async Task<TestDto> GetAsync(Guid id)
    {
        return await _testAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Tests.Create)]
    public virtual async Task<TestDto> CreateAsync([FromBody] CreateTestDto input)
    {
        return await _testAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Tests.Edit)]
    public virtual async Task<TestDto> UpdateAsync(Guid id, [FromBody] UpdateTestDto input)
    {
        return await _testAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Tests.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        await _testAppService.DeleteAsync(id);
    }

    [HttpGet("by-specimen/{specimenId}")]
    public virtual async Task<ListResultDto<TestDto>> GetBySpecimenAsync(Guid specimenId)
    {
        return await _testAppService.GetBySpecimenAsync(specimenId);
    }
}
