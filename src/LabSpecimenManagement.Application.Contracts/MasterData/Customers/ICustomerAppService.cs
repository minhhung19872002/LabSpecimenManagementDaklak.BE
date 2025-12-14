using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface ICustomerAppService : ICrudAppService<
    CustomerDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateCustomerDto>
{
    Task<ListResultDto<CustomerDto>> GetActiveCustomersAsync();
}
