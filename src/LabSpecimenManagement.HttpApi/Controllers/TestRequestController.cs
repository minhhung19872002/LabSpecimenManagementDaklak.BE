using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Area("app")]
[Route("api/app/test-request")]
public class TestRequestController : AbpControllerBase
{
    private readonly ITestRequestAppService _testRequestAppService;

    public TestRequestController(ITestRequestAppService testRequestAppService)
    {
        _testRequestAppService = testRequestAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<TestRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return _testRequestAppService.GetListAsync(input);
    }

    [HttpGet]
    [Route("{id}")]
    public virtual Task<TestRequestDto> GetAsync(Guid id)
    {
        return _testRequestAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual Task<TestRequestDto> CreateAsync(CreateTestRequestDto input)
    {
        return _testRequestAppService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public virtual Task<TestRequestDto> UpdateAsync(Guid id, UpdateTestRequestDto input)
    {
        return _testRequestAppService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testRequestAppService.DeleteAsync(id);
    }
}
