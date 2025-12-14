using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface IDepartmentAppService : ICrudAppService<
    DepartmentDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateDepartmentDto>
{
    Task<ListResultDto<DepartmentDto>> GetActiveDepartmentsAsync();
}
