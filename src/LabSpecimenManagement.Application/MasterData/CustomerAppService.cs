using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
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
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;

    public CustomerAppService(
        IRepository<Customer, Guid> repository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter)
        : base(repository)
    {
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
    }

    protected override async Task<Customer> MapToEntityAsync(CreateUpdateCustomerDto createInput)
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        int count;
        using (_dataFilter.Disable<ISoftDelete>())
        {
            count = await Repository.CountAsync();
        }
        var code = _codeGenerator.GenerateCode("CUST", count);
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
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<CustomerDto>(
            ObjectMapper.Map<List<Customer>, List<CustomerDto>>(items)
        );
    }
}
