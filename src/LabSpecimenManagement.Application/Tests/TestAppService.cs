using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.TestRequests;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

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
    private readonly IUserNameProvider _userNameProvider;
    private readonly ICodeGeneratorService _codeGenerator;

    public TestAppService(
        IRepository<Test, Guid> repository,
        IRepository<Specimen, Guid> specimenRepository,
        IUserNameProvider userNameProvider,
        ICodeGeneratorService codeGenerator)
        : base(repository)
    {
        _specimenRepository = specimenRepository;
        _userNameProvider = userNameProvider;
        _codeGenerator = codeGenerator;
    }

    protected override async Task<Test> MapToEntityAsync(CreateTestDto createInput)
    {
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

    public async Task<PagedResultDto<TestDto>> GetFilteredListAsync(GetTestListDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.Specimen);

        // Apply filters using extension methods
        var search = input.Search?.NormalizeSearch();
        query = query
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                x.Code.ToLower().Contains(search!) ||
                x.Name.ToLower().Contains(search!) ||
                x.Method.ToLower().Contains(search!) ||
                x.Standard.ToLower().Contains(search!) ||
                (x.Specimen != null && x.Specimen.Code.ToLower().Contains(search!)))
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(input.SpecimenId.HasValue, x => x.SpecimenId == input.SpecimenId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Method), x => x.Method == input.Method)
            .WhereIf(input.AssigneeId.HasValue, x => x.AssigneeId == input.AssigneeId!.Value);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query.ApplySorting(input.Sorting, x => x.CreationTime, defaultDescending: true);
        query = query.ApplyPaging(input);

        var entities = await AsyncExecuter.ToListAsync(query);
        var dtos = ObjectMapper.Map<List<Test>, List<TestDto>>(entities);

        await EnrichTestDtosAsync(dtos, entities);

        return new PagedResultDto<TestDto>(totalCount, dtos);
    }

    public override async Task<PagedResultDto<TestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.Specimen);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query
            .ApplySorting(input.Sorting, x => x.CreationTime, defaultDescending: true)
            .ApplyPaging(input);

        var entities = await AsyncExecuter.ToListAsync(query);
        var dtos = ObjectMapper.Map<List<Test>, List<TestDto>>(entities);

        await EnrichTestDtosAsync(dtos, entities);

        return new PagedResultDto<TestDto>(totalCount, dtos);
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

        dto.AssigneeName = await _userNameProvider.GetUserNameAsync(entity.AssigneeId);

        return dto;
    }

    public async Task<ListResultDto<TestDto>> GetBySpecimenAsync(Guid specimenId)
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.SpecimenId == specimenId)
            .OrderBy(x => x.Code)
            .ToList();

        return new ListResultDto<TestDto>(
            ObjectMapper.Map<List<Test>, List<TestDto>>(items)
        );
    }

    /// <summary>
    /// Generates test code with date prefix
    /// </summary>
    private async Task<string> GenerateTestCodeAsync()
    {
        var prefix = _codeGenerator.GetTodayPrefix("TEST");
        var count = await Repository.CountAsync(x => x.Code.StartsWith(prefix));
        return _codeGenerator.GenerateCodeWithDate("TEST", count);
    }

    /// <summary>
    /// Enriches test DTOs with specimen and assignee information
    /// </summary>
    private async Task EnrichTestDtosAsync(List<TestDto> dtos, List<Test> entities)
    {
        // Enrich with specimen information
        foreach (var dto in dtos)
        {
            var entity = entities.FirstOrDefault(x => x.Id == dto.Id);
            if (entity?.Specimen != null)
            {
                dto.SpecimenCode = entity.Specimen.Code;
                dto.SpecimenName = entity.Specimen.Name;
            }
        }

        // Enrich with assignee names
        await _userNameProvider.EnrichWithUserNamesAsync(
            dtos,
            dto => entities.FirstOrDefault(e => e.Id == dto.Id)?.AssigneeId,
            (dto, name) => dto.AssigneeName = name);
    }
}
