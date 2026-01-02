using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class DepartmentAppService :
    CrudAppService<
        Department,
        DepartmentDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateDepartmentDto>,
    IDepartmentAppService
{
    public DepartmentAppService(IRepository<Department, Guid> repository)
        : base(repository)
    {
    }

    public async Task<ListResultDto<DepartmentDto>> GetActiveDepartmentsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderByThenBy(x => x.SortOrder, x => x.Name)
            .ToList();

        return new ListResultDto<DepartmentDto>(
            ObjectMapper.Map<List<Department>, List<DepartmentDto>>(items)
        );
    }
}
