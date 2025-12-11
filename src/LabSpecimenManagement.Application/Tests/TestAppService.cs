using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Entities;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Tests;

[RemoteService(IsEnabled = false)]
public class TestAppService :
    CrudAppService<
        Test,
        TestDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTestDto,
        UpdateTestDto>,
    ITestAppService
{
    private readonly IRepository<Specimen, Guid> _specimenRepository;

    public TestAppService(
        IRepository<Test, Guid> repository,
        IRepository<Specimen, Guid> specimenRepository)
        : base(repository)
    {
        _specimenRepository = specimenRepository;
    }

    protected override async Task<Test> MapToEntityAsync(CreateTestDto createInput)
    {
        // Generate test code automatically
        var testCode = await GenerateTestCodeAsync();

        var entity = new Test(
            GuidGenerator.Create(),
            createInput.SpecimenId,
            testCode,
            createInput.Name,
            createInput.Method,
            createInput.Standard
        );

        entity.AssigneeId = createInput.AssigneeId;
        entity.EstimatedDuration = createInput.EstimatedDuration;
        entity.Notes = createInput.Notes;

        return entity;
    }

    public override async Task<PagedResultDto<TestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.Specimen);

        query = ApplyDefaultSorting(query);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = ApplySorting(query, input);
        query = ApplyPaging(query, input);

        var entities = await AsyncExecuter.ToListAsync(query);

        var dtos = ObjectMapper.Map<System.Collections.Generic.List<Test>, System.Collections.Generic.List<TestDto>>(entities);

        foreach (var dto in dtos)
        {
            var entity = entities.FirstOrDefault(x => x.Id == dto.Id);
            if (entity?.Specimen != null)
            {
                dto.SpecimenCode = entity.Specimen.Code;
                dto.SpecimenName = entity.Specimen.Name;
            }
        }

        return new PagedResultDto<TestDto>(
            totalCount,
            dtos
        );
    }

    public override async Task<TestDto> GetAsync(Guid id)
    {
        var query = await Repository.WithDetailsAsync(x => x.Specimen);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (entity == null)
        {
            throw new EntityNotFoundException(typeof(Test), id);
        }

        var dto = ObjectMapper.Map<Test, TestDto>(entity);

        if (entity.Specimen != null)
        {
            dto.SpecimenCode = entity.Specimen.Code;
            dto.SpecimenName = entity.Specimen.Name;
        }

        return dto;
    }

    private async Task<string> GenerateTestCodeAsync()
    {
        // Generate test code format: TEST-YYYYMMDD-XXXX
        var today = DateTime.Now;
        var prefix = $"TEST-{today:yyyyMMdd}";

        var count = await Repository.CountAsync(x => x.Code.StartsWith(prefix));
        var sequence = (count + 1).ToString("D4");

        return $"{prefix}-{sequence}";
    }
}
