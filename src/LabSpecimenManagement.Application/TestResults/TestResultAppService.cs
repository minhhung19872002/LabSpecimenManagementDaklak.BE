using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.TestResults;

[RemoteService(IsEnabled = false)]
public class TestResultAppService : CrudAppService<
    TestResult,
    TestResultDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateTestResultDto,
    UpdateTestResultDto>, ITestResultAppService
{
    public TestResultAppService(IRepository<TestResult, Guid> repository)
        : base(repository)
    {
    }

    public override async Task<PagedResultDto<TestResultDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Test);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = ApplyPaging(queryable, input);
        queryable = ApplySorting(queryable, input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = entities.Select(entity =>
        {
            var dto = ObjectMapper.Map<TestResult, TestResultDto>(entity);
            if (entity.Test != null)
            {
                dto.TestCode = entity.Test.Code;
                dto.TestName = entity.Test.Name;
            }
            return dto;
        }).ToList();

        return new PagedResultDto<TestResultDto>(totalCount, dtos);
    }

    public override async Task<TestResultDto> GetAsync(Guid id)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Test);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(x => x.Id == id)
        );

        if (entity == null)
        {
            throw new Volo.Abp.Domain.Entities.EntityNotFoundException(typeof(TestResult), id);
        }

        var dto = ObjectMapper.Map<TestResult, TestResultDto>(entity);
        if (entity.Test != null)
        {
            dto.TestCode = entity.Test.Code;
            dto.TestName = entity.Test.Name;
        }

        return dto;
    }
}
