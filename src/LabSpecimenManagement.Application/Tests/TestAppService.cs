using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Identity;
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
    private readonly IIdentityUserRepository _userRepository;

    public TestAppService(
        IRepository<Test, Guid> repository,
        IRepository<Specimen, Guid> specimenRepository,
        IIdentityUserRepository userRepository)
        : base(repository)
    {
        _specimenRepository = specimenRepository;
        _userRepository = userRepository;
    }

    private async Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var validIds = userIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (validIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var users = await _userRepository.GetListAsync();
        return users
            .Where(u => validIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => $"{u.Surname} {u.Name}".Trim());
    }

    private async Task<string?> GetUserNameAsync(Guid? userId)
    {
        if (!userId.HasValue) return null;

        var user = await _userRepository.FindAsync(userId.Value);
        if (user == null) return null;

        return $"{user.Surname} {user.Name}".Trim();
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

    public async Task<PagedResultDto<TestDto>> GetFilteredListAsync(GetTestListDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.Specimen);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                x.Method.ToLower().Contains(search) ||
                x.Standard.ToLower().Contains(search) ||
                (x.Specimen != null && x.Specimen.Code.ToLower().Contains(search)));
        }

        // Apply status filter
        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        // Apply specimen filter
        if (input.SpecimenId.HasValue)
        {
            query = query.Where(x => x.SpecimenId == input.SpecimenId.Value);
        }

        // Apply method filter
        if (!string.IsNullOrWhiteSpace(input.Method))
        {
            query = query.Where(x => x.Method == input.Method);
        }

        // Apply assignee filter
        if (input.AssigneeId.HasValue)
        {
            query = query.Where(x => x.AssigneeId == input.AssigneeId.Value);
        }

        // Get total count
        var totalCount = await AsyncExecuter.CountAsync(query);

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            query = query.OrderBy(input.Sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.CreationTime);
        }

        // Apply paging
        query = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(query);

        // Get user names for assigneeId
        var userNames = await GetUserNamesAsync(entities.Select(e => e.AssigneeId));

        var dtos = ObjectMapper.Map<List<Test>, List<TestDto>>(entities);

        foreach (var dto in dtos)
        {
            var entity = entities.FirstOrDefault(x => x.Id == dto.Id);
            if (entity?.Specimen != null)
            {
                dto.SpecimenCode = entity.Specimen.Code;
                dto.SpecimenName = entity.Specimen.Name;
            }
            if (entity?.AssigneeId.HasValue == true && userNames.TryGetValue(entity.AssigneeId.Value, out var userName))
            {
                dto.AssigneeName = userName;
            }
        }

        return new PagedResultDto<TestDto>(totalCount, dtos);
    }

    public override async Task<PagedResultDto<TestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.Specimen);

        query = ApplyDefaultSorting(query);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = ApplySorting(query, input);
        query = ApplyPaging(query, input);

        var entities = await AsyncExecuter.ToListAsync(query);

        // Get user names for assigneeId
        var userNames = await GetUserNamesAsync(entities.Select(e => e.AssigneeId));

        var dtos = ObjectMapper.Map<System.Collections.Generic.List<Test>, System.Collections.Generic.List<TestDto>>(entities);

        foreach (var dto in dtos)
        {
            var entity = entities.FirstOrDefault(x => x.Id == dto.Id);
            if (entity?.Specimen != null)
            {
                dto.SpecimenCode = entity.Specimen.Code;
                dto.SpecimenName = entity.Specimen.Name;
            }
            if (entity?.AssigneeId.HasValue == true && userNames.TryGetValue(entity.AssigneeId.Value, out var userName))
            {
                dto.AssigneeName = userName;
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

        // Get user name for assigneeId
        dto.AssigneeName = await GetUserNameAsync(entity.AssigneeId);

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

    public async Task<ListResultDto<TestDto>> GetBySpecimenAsync(Guid specimenId)
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.SpecimenId == specimenId)
            .OrderBy(x => x.Code)
            .ToList();

        return new ListResultDto<TestDto>(
            ObjectMapper.Map<System.Collections.Generic.List<Test>, System.Collections.Generic.List<TestDto>>(items)
        );
    }
}
