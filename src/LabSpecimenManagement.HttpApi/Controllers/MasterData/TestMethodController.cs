using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/test-methods")]
[Authorize(LabSpecimenManagementPermissions.TestMethods.Default)]
public class TestMethodController : LabSpecimenManagementController
{
    private readonly ITestMethodAppService _testMethodAppService;

    public TestMethodController(ITestMethodAppService testMethodAppService)
    {
        _testMethodAppService = testMethodAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestMethodDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _testMethodAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<TestMethodDto> GetAsync(Guid id)
    {
        return _testMethodAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.TestMethods.Create)]
    public virtual Task<TestMethodDto> CreateAsync([FromBody] CreateUpdateTestMethodDto input)
    {
        return _testMethodAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestMethods.Edit)]
    public virtual Task<TestMethodDto> UpdateAsync(Guid id, [FromBody] CreateUpdateTestMethodDto input)
    {
        return _testMethodAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestMethods.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testMethodAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<TestMethodDto>> GetActiveTestMethodsAsync()
    {
        return _testMethodAppService.GetActiveTestMethodsAsync();
    }

    [HttpGet("by-department/{departmentId}")]
    public virtual Task<ListResultDto<TestMethodDto>> GetByDepartmentAsync(Guid departmentId)
    {
        return _testMethodAppService.GetByDepartmentAsync(departmentId);
    }
}
