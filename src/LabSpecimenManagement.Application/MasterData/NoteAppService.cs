using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.Notes;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class NoteAppService : ApplicationService, INoteAppService
{
    private const string CodePrefix = "GC";
    private readonly IRepository<Note, Guid> _repository;
    private readonly IDataFilter _dataFilter;

    public NoteAppService(
        IRepository<Note, Guid> repository,
        IDataFilter dataFilter)
    {
        _repository = repository;
        _dataFilter = dataFilter;
    }

    public async Task<PagedResultDto<NoteDto>> GetListAsync(NoteFilterDto input)
    {
        var query = await _repository.GetQueryableAsync();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var searchLower = input.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(searchLower) ||
                x.Content.ToLower().Contains(searchLower)
            );
        }

        // Apply status filter
        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .OrderBy(x => x.SortOrder)
                .ThenByDescending(x => x.CreationTime)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
        );

        return new PagedResultDto<NoteDto>(
            totalCount,
            ObjectMapper.Map<List<Note>, List<NoteDto>>(items)
        );
    }

    public async Task<NoteDto> GetAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        return ObjectMapper.Map<Note, NoteDto>(entity);
    }

    public async Task<NoteDto> CreateAsync(CreateNoteDto input)
    {
        var code = await GenerateCodeAsync();

        var entity = new Note(
            GuidGenerator.Create(),
            code,
            input.Content
        )
        {
            IsActive = input.IsActive,
            SortOrder = input.SortOrder
        };

        await _repository.InsertAsync(entity);

        return ObjectMapper.Map<Note, NoteDto>(entity);
    }

    public async Task<NoteDto> UpdateAsync(Guid id, UpdateNoteDto input)
    {
        var entity = await _repository.GetAsync(id);

        entity.Content = input.Content;
        entity.IsActive = input.IsActive;
        entity.SortOrder = input.SortOrder;

        await _repository.UpdateAsync(entity);

        return ObjectMapper.Map<Note, NoteDto>(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<NoteDto> ToggleActiveAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        entity.IsActive = !entity.IsActive;
        await _repository.UpdateAsync(entity);
        return ObjectMapper.Map<Note, NoteDto>(entity);
    }

    public async Task<List<NoteDto>> GetActiveNotesAsync()
    {
        var query = await _repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            query
                .WhereActive()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Content)
        );

        return ObjectMapper.Map<List<Note>, List<NoteDto>>(items);
    }

    /// <summary>
    /// Generate code with format GC-XXXX
    /// </summary>
    private async Task<string> GenerateCodeAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _repository.GetQueryableAsync();
            var codes = await AsyncExecuter.ToListAsync(
                query
                    .Where(x => x.Code.StartsWith($"{CodePrefix}-"))
                    .Select(x => x.Code)
            );

            int maxNumber = 0;
            foreach (var code in codes)
            {
                var numberPart = code.Replace($"{CodePrefix}-", "");
                if (int.TryParse(numberPart, out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            var nextNumber = maxNumber + 1;
            return $"{CodePrefix}-{nextNumber:D4}";
        }
    }
}
