using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.SpecimenHistories;

[RemoteService(IsEnabled = false)]
public class SpecimenHistoryAppService : CrudAppService<
    SpecimenHistory,
    SpecimenHistoryDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateSpecimenHistoryDto,
    UpdateSpecimenHistoryDto>, ISpecimenHistoryAppService
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IUserNameProvider _userNameProvider;

    public SpecimenHistoryAppService(
        IRepository<SpecimenHistory, Guid> repository,
        IGuidGenerator guidGenerator,
        IUserNameProvider userNameProvider)
        : base(repository)
    {
        _guidGenerator = guidGenerator;
        _userNameProvider = userNameProvider;
    }

    protected override Task<SpecimenHistory> MapToEntityAsync(CreateSpecimenHistoryDto createInput)
    {
        var entity = new SpecimenHistory(
            _guidGenerator.Create(),
            createInput.SpecimenId,
            createInput.Action,
            createInput.Location,
            createInput.Status
        );

        entity.Notes = createInput.Notes;
        entity.Temperature = createInput.Temperature;
        entity.Humidity = createInput.Humidity;
        entity.Timestamp = createInput.Timestamp;
        entity.PerformedById = createInput.PerformedById;

        return Task.FromResult(entity);
    }

    protected override async Task MapToEntityAsync(UpdateSpecimenHistoryDto updateInput, SpecimenHistory entity)
    {
        entity.Action = updateInput.Action;
        entity.Location = updateInput.Location;
        entity.Status = updateInput.Status;
        entity.Notes = updateInput.Notes;
        entity.Temperature = updateInput.Temperature;
        entity.Humidity = updateInput.Humidity;
        entity.Timestamp = updateInput.Timestamp;
        entity.PerformedById = updateInput.PerformedById;

        await Task.CompletedTask;
    }

    public async Task<PagedResultDto<SpecimenHistoryDto>> GetFilteredListAsync(GetSpecimenHistoryListDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.Specimen);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(x =>
                (x.Specimen != null && x.Specimen.Code.ToLower().Contains(search)) ||
                x.Action.ToLower().Contains(search) ||
                x.Location.ToLower().Contains(search) ||
                (x.Notes != null && x.Notes.ToLower().Contains(search)));
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

        // Apply action filter
        if (!string.IsNullOrWhiteSpace(input.Action))
        {
            query = query.Where(x => x.Action == input.Action);
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

        // Get user names for performedById
        var userNames = await _userNameProvider.GetUserNamesAsync(entities.Select(e => e.PerformedById));

        var dtos = entities.Select(entity =>
        {
            var dto = ObjectMapper.Map<SpecimenHistory, SpecimenHistoryDto>(entity);
            if (entity.Specimen != null)
            {
                dto.SpecimenCode = entity.Specimen.Code;
            }
            if (entity.PerformedById.HasValue && userNames.TryGetValue(entity.PerformedById.Value, out var userName))
            {
                dto.PerformedByName = userName;
            }
            return dto;
        }).ToList();

        return new PagedResultDto<SpecimenHistoryDto>(totalCount, dtos);
    }

    public async Task<ListResultDto<SpecimenHistoryDto>> GetBySpecimenAsync(Guid specimenId)
    {
        var query = await Repository.WithDetailsAsync(x => x.Specimen);

        var items = query
            .Where(x => x.SpecimenId == specimenId)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        // Get user names for performedById
        var userNames = await _userNameProvider.GetUserNamesAsync(items.Select(e => e.PerformedById));

        var dtos = items.Select(entity =>
        {
            var dto = ObjectMapper.Map<SpecimenHistory, SpecimenHistoryDto>(entity);
            if (entity.Specimen != null)
            {
                dto.SpecimenCode = entity.Specimen.Code;
            }
            if (entity.PerformedById.HasValue && userNames.TryGetValue(entity.PerformedById.Value, out var userName))
            {
                dto.PerformedByName = userName;
            }
            return dto;
        }).ToList();

        return new ListResultDto<SpecimenHistoryDto>(dtos);
    }

    public override async Task<PagedResultDto<SpecimenHistoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Specimen);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = ApplyPaging(queryable, input);
        queryable = ApplySorting(queryable, input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        // Get user names for performedById
        var userNames = await _userNameProvider.GetUserNamesAsync(entities.Select(e => e.PerformedById));

        var dtos = entities.Select(entity =>
        {
            var dto = ObjectMapper.Map<SpecimenHistory, SpecimenHistoryDto>(entity);
            if (entity.Specimen != null)
            {
                dto.SpecimenCode = entity.Specimen.Code;
            }
            if (entity.PerformedById.HasValue && userNames.TryGetValue(entity.PerformedById.Value, out var userName))
            {
                dto.PerformedByName = userName;
            }
            return dto;
        }).ToList();

        return new PagedResultDto<SpecimenHistoryDto>(totalCount, dtos);
    }

    public override async Task<SpecimenHistoryDto> GetAsync(Guid id)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Specimen);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(x => x.Id == id)
        );

        if (entity == null)
        {
            throw new Volo.Abp.Domain.Entities.EntityNotFoundException(typeof(SpecimenHistory), id);
        }

        var dto = ObjectMapper.Map<SpecimenHistory, SpecimenHistoryDto>(entity);
        if (entity.Specimen != null)
        {
            dto.SpecimenCode = entity.Specimen.Code;
        }

        // Get user name for performedById
        dto.PerformedByName = await _userNameProvider.GetUserNameAsync(entity.PerformedById);

        return dto;
    }
}
