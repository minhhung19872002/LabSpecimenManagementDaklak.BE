using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface ITestMethodAppService : ICrudAppService<
    TestMethodDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateTestMethodDto>
{
    Task<ListResultDto<TestMethodDto>> GetActiveTestMethodsAsync();
    Task<ListResultDto<TestMethodDto>> GetByDepartmentAsync(Guid departmentId);
}
