using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class CustomerAppService :
    CrudAppService<
        Customer,
        CustomerDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateCustomerDto>,
    ICustomerAppService
{
    public CustomerAppService(IRepository<Customer, Guid> repository)
        : base(repository)
    {
    }

    protected override async Task<Customer> MapToEntityAsync(CreateUpdateCustomerDto createInput)
    {
        var code = await GenerateCustomerCodeAsync();
        var entity = new Customer(GuidGenerator.Create(), code, createInput.Name);

        entity.Email = createInput.Email;
        entity.Phone = createInput.Phone;
        entity.Address = createInput.Address;
        entity.TaxCode = createInput.TaxCode;
        entity.ContactPerson = createInput.ContactPerson;
        entity.ContactPhone = createInput.ContactPhone;
        entity.Notes = createInput.Notes;
        entity.IsActive = createInput.IsActive;

        return entity;
    }

    public async Task<ListResultDto<CustomerDto>> GetActiveCustomersAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<CustomerDto>(
            ObjectMapper.Map<List<Customer>, List<CustomerDto>>(items)
        );
    }

    private async Task<string> GenerateCustomerCodeAsync()
    {
        var prefix = "CUST";
        var count = await Repository.CountAsync();
        var sequence = (count + 1).ToString("D4");
        return $"{prefix}-{sequence}";
    }
}
