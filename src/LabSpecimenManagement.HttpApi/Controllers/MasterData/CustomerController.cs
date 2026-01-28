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
[Route("api/v1/customers")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class CustomerController : LabSpecimenManagementController
{
    private readonly ICustomerAppService _customerAppService;

    public CustomerController(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<CustomerDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _customerAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<CustomerDto> GetAsync(Guid id)
    {
        return _customerAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<CustomerDto> CreateAsync([FromBody] CreateUpdateCustomerDto input)
    {
        return _customerAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<CustomerDto> UpdateAsync(Guid id, [FromBody] CreateUpdateCustomerDto input)
    {
        return _customerAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _customerAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<CustomerDto>> GetActiveCustomersAsync()
    {
        return _customerAppService.GetActiveCustomersAsync();
    }
}
