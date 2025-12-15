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
public class TestMethodAppService :
    CrudAppService<
        TestMethod,
        TestMethodDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateTestMethodDto>,
    ITestMethodAppService
{
    public TestMethodAppService(IRepository<TestMethod, Guid> repository)
        : base(repository)
    {
    }

    public async Task<ListResultDto<TestMethodDto>> GetActiveTestMethodsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<TestMethodDto>(
            ObjectMapper.Map<List<TestMethod>, List<TestMethodDto>>(items)
        );
    }

    public async Task<ListResultDto<TestMethodDto>> GetByDepartmentAsync(Guid departmentId)
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.DepartmentId == departmentId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<TestMethodDto>(
            ObjectMapper.Map<List<TestMethod>, List<TestMethodDto>>(items)
        );
    }
}
