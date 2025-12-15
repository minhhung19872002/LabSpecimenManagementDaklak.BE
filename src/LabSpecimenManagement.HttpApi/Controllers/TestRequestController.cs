using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/test-requests")]
[Authorize(LabSpecimenManagementPermissions.TestRequests.Default)]
public class TestRequestController : LabSpecimenManagementController
{
    private readonly ITestRequestAppService _testRequestAppService;

    public TestRequestController(ITestRequestAppService testRequestAppService)
    {
        _testRequestAppService = testRequestAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestRequestDto>> GetListAsync([FromQuery] GetTestRequestListDto input)
    {
        return await _testRequestAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<TestRequestDto> GetAsync(Guid id)
    {
        return _testRequestAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Create)]
    public virtual Task<TestRequestDto> CreateAsync([FromBody] CreateTestRequestDto input)
    {
        return _testRequestAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task<TestRequestDto> UpdateAsync(Guid id, [FromBody] UpdateTestRequestDto input)
    {
        return _testRequestAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testRequestAppService.DeleteAsync(id);
    }
}
