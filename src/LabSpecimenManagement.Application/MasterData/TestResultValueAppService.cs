using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class TestResultValueAppService : ApplicationService, ITestResultValueAppService
{
    private readonly IRepository<TestResultValue, Guid> _repository;
    private readonly IDataFilter _dataFilter;

    public TestResultValueAppService(
        IRepository<TestResultValue, Guid> repository,
        IDataFilter dataFilter)
    {
        _repository = repository;
        _dataFilter = dataFilter;
    }

    public async Task<PagedResultDto<TestResultValueDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await _repository.GetQueryableAsync();

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .OrderByDescending(x => x.CreationTime)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
        );

        return new PagedResultDto<TestResultValueDto>(
            totalCount,
            ObjectMapper.Map<List<TestResultValue>, List<TestResultValueDto>>(items)
        );
    }

    public async Task<TestResultValueDto> GetAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        return ObjectMapper.Map<TestResultValue, TestResultValueDto>(entity);
    }

    public async Task<TestResultValueDto> CreateAsync(CreateUpdateTestResultValueDto input)
    {
        // Check if name already exists
        await ValidateNameUniqueAsync(input.Name);

        var code = await GenerateCodeAsync();

        var entity = new TestResultValue(
            GuidGenerator.Create(),
            code,
            input.Name
        )
        {
            Description = input.Description,
            IsActive = true,
            SortOrder = 0
        };

        await _repository.InsertAsync(entity);

        return ObjectMapper.Map<TestResultValue, TestResultValueDto>(entity);
    }

    public async Task<TestResultValueDto> UpdateAsync(Guid id, CreateUpdateTestResultValueDto input)
    {
        var entity = await _repository.GetAsync(id);

        // Check if name already exists (exclude current entity)
        await ValidateNameUniqueAsync(input.Name, id);

        entity.Name = input.Name;
        entity.Description = input.Description;

        await _repository.UpdateAsync(entity);

        return ObjectMapper.Map<TestResultValue, TestResultValueDto>(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// Validate that the name is unique
    /// </summary>
    private async Task ValidateNameUniqueAsync(string name, Guid? excludeId = null)
    {
        var query = await _repository.GetQueryableAsync();
        var exists = query.Any(x =>
            x.Name.ToLower() == name.ToLower() &&
            (excludeId == null || x.Id != excludeId));

        if (exists)
        {
            throw new UserFriendlyException($"Tên kết quả '{name}' đã tồn tại. Vui lòng nhập tên khác.");
        }
    }

    public async Task<ListResultDto<TestResultValueDto>> GetActiveTestResultValuesAsync()
    {
        var query = await _repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            query
                .WhereActive()
                .OrderBy(x => x.Name)
        );

        return new ListResultDto<TestResultValueDto>(
            ObjectMapper.Map<List<TestResultValue>, List<TestResultValueDto>>(items)
        );
    }

    /// <summary>
    /// Get or create a test result value by name
    /// Used for auto-creating text values when submitting results for approval
    /// </summary>
    public async Task<TestResultValueDto?> GetOrCreateByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        // Tìm existing (case-insensitive)
        var query = await _repository.GetQueryableAsync();
        var existing = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(x => x.Name.ToLower() == name.ToLower())
        );

        if (existing != null)
        {
            return ObjectMapper.Map<TestResultValue, TestResultValueDto>(existing);
        }

        // Tạo mới
        return await CreateAsync(new CreateUpdateTestResultValueDto { Name = name });
    }

    /// <summary>
    /// Generate code with format KQddmmyyyyxxx
    /// </summary>
    private async Task<string> GenerateCodeAsync()
    {
        var today = DateTime.Now;
        var datePrefix = $"KQ{today:ddMMyyyy}";

        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _repository.GetQueryableAsync();
            var maxCode = await AsyncExecuter.FirstOrDefaultAsync(
                query
                    .Where(x => x.Code.StartsWith(datePrefix))
                    .Select(x => x.Code)
                    .OrderByDescending(x => x)
            );

            int nextSeq = 1;
            if (maxCode != null)
            {
                var seqPart = maxCode.Substring(datePrefix.Length);
                if (int.TryParse(seqPart, out var seq))
                {
                    nextSeq = seq + 1;
                }
            }

            if (nextSeq > 999)
            {
                throw new UserFriendlyException("Đã vượt quá số lượng mã kết quả cho phép trong ngày (999)");
            }

            return $"{datePrefix}{nextSeq:D3}";
        }
    }
}
