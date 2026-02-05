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
    private readonly IRepository<TestResultValueIndicator, Guid> _junctionRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IDataFilter _dataFilter;

    public TestResultValueAppService(
        IRepository<TestResultValue, Guid> repository,
        IRepository<TestResultValueIndicator, Guid> junctionRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IDataFilter dataFilter)
    {
        _repository = repository;
        _junctionRepository = junctionRepository;
        _indicatorRepository = indicatorRepository;
        _specimenTypeRepository = specimenTypeRepository;
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

        var dtos = ObjectMapper.Map<List<TestResultValue>, List<TestResultValueDto>>(items);

        // Load indicator counts from junction table
        var junctionQuery = await _junctionRepository.GetQueryableAsync();
        var resultValueIds = items.Select(x => x.Id).ToList();
        var indicatorCounts = junctionQuery
            .Where(j => resultValueIds.Contains(j.TestResultValueId))
            .GroupBy(j => j.TestResultValueId)
            .Select(g => new { TestResultValueId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.TestResultValueId, x => x.Count);

        foreach (var dto in dtos)
        {
            dto.IndicatorCount = indicatorCounts.GetValueOrDefault(dto.Id, 0);
        }

        return new PagedResultDto<TestResultValueDto>(totalCount, dtos);
    }

    public async Task<TestResultValueDto> GetAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        var dto = ObjectMapper.Map<TestResultValue, TestResultValueDto>(entity);

        // Load indicator count
        var junctionQuery = await _junctionRepository.GetQueryableAsync();
        dto.IndicatorCount = junctionQuery.Count(j => j.TestResultValueId == id);

        return dto;
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

        await _repository.InsertAsync(entity, autoSave: true);

        var dto = ObjectMapper.Map<TestResultValue, TestResultValueDto>(entity);
        dto.IndicatorCount = 0;

        return dto;
    }

    public async Task<TestResultValueDto> UpdateAsync(Guid id, CreateUpdateTestResultValueDto input)
    {
        var entity = await _repository.GetAsync(id);

        // Check if name already exists (exclude current entity)
        await ValidateNameUniqueAsync(input.Name, id);

        entity.Name = input.Name;
        entity.Description = input.Description;

        await _repository.UpdateAsync(entity);

        var dto = ObjectMapper.Map<TestResultValue, TestResultValueDto>(entity);

        // Load indicator count
        var junctionQuery = await _junctionRepository.GetQueryableAsync();
        dto.IndicatorCount = junctionQuery.Count(j => j.TestResultValueId == id);

        return dto;
    }

    public async Task DeleteAsync(Guid id)
    {
        // Also delete junction records
        var junctionQuery = await _junctionRepository.GetQueryableAsync();
        var junctions = junctionQuery.Where(j => j.TestResultValueId == id).ToList();
        if (junctions.Any())
        {
            await _junctionRepository.DeleteManyAsync(junctions);
        }

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

        var dtos = ObjectMapper.Map<List<TestResultValue>, List<TestResultValueDto>>(items);

        // Load indicator counts
        var junctionQuery = await _junctionRepository.GetQueryableAsync();
        var resultValueIds = items.Select(x => x.Id).ToList();
        var indicatorCounts = junctionQuery
            .Where(j => resultValueIds.Contains(j.TestResultValueId))
            .GroupBy(j => j.TestResultValueId)
            .Select(g => new { TestResultValueId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.TestResultValueId, x => x.Count);

        foreach (var dto in dtos)
        {
            dto.IndicatorCount = indicatorCounts.GetValueOrDefault(dto.Id, 0);
        }

        return new ListResultDto<TestResultValueDto>(dtos);
    }

    /// <summary>
    /// Get or create a test result value by name
    /// Used for auto-creating text values when submitting results for approval
    /// Includes retry mechanism for handling concurrent insert race conditions
    /// </summary>
    public async Task<TestResultValueDto?> GetOrCreateByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            // Tìm existing (case-insensitive)
            var query = await _repository.GetQueryableAsync();
            var existing = await AsyncExecuter.FirstOrDefaultAsync(
                query.Where(x => x.Name.ToLower() == name.ToLower())
            );

            if (existing != null)
            {
                return ObjectMapper.Map<TestResultValue, TestResultValueDto>(existing);
            }

            // Tạo mới - wrap in try-catch to handle race condition
            try
            {
                return await CreateAsync(new CreateUpdateTestResultValueDto { Name = name });
            }
            catch (Volo.Abp.UserFriendlyException ex) when (ex.Message.Contains("đã tồn tại"))
            {
                // Name was created by another concurrent request, continue to retry and fetch it
                if (attempt == maxRetries - 1) throw;
                await Task.Delay(10 * (attempt + 1)); // Small backoff
                continue;
            }
            catch (Exception ex) when (IsDuplicateKeyException(ex))
            {
                // Duplicate key constraint violation - another concurrent request created the same code
                // Retry to fetch the newly created record or create with a new code
                if (attempt == maxRetries - 1) throw;
                await Task.Delay(10 * (attempt + 1)); // Small backoff
                continue;
            }
        }

        // Final attempt - just return existing if found
        var finalQuery = await _repository.GetQueryableAsync();
        var finalExisting = await AsyncExecuter.FirstOrDefaultAsync(
            finalQuery.Where(x => x.Name.ToLower() == name.ToLower())
        );
        return finalExisting != null ? ObjectMapper.Map<TestResultValue, TestResultValueDto>(finalExisting) : null;
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

    /// <summary>
    /// Check if exception is a duplicate key constraint violation
    /// Works with PostgreSQL (23505) and other databases
    /// </summary>
    private static bool IsDuplicateKeyException(Exception ex)
    {
        // Check exception type name (avoid direct reference to EF Core)
        if (ex.GetType().Name == "DbUpdateException")
        {
            var innerMessage = ex.InnerException?.Message ?? "";
            return innerMessage.Contains("duplicate key") ||
                   innerMessage.Contains("23505") ||
                   innerMessage.Contains("unique constraint");
        }

        // Also check if it's wrapped in an AbpDbConcurrencyException
        if (ex.InnerException != null)
        {
            return IsDuplicateKeyException(ex.InnerException);
        }

        return false;
    }

    /// <summary>
    /// Lấy danh sách kết quả recommend cho chỉ tiêu
    /// - Nếu chỉ tiêu có kết quả riêng (trong junction table): chỉ trả về kết quả của chỉ tiêu đó
    /// - Nếu chỉ tiêu không có kết quả riêng: trả về tất cả kết quả không gắn chỉ tiêu
    /// </summary>
    public async Task<ListResultDto<TestResultValueDto>> GetResultsForIndicatorAsync(Guid indicatorId)
    {
        var query = await _repository.GetQueryableAsync();
        var junctionQuery = await _junctionRepository.GetQueryableAsync();

        // Kiểm tra xem chỉ tiêu này có kết quả riêng không (trong junction table)
        var resultValueIdsForIndicator = junctionQuery
            .Where(j => j.IndicatorId == indicatorId && j.IsActive)
            .Select(j => j.TestResultValueId)
            .Distinct()
            .ToList();

        List<TestResultValue> items;
        if (resultValueIdsForIndicator.Any())
        {
            // Chỉ trả về kết quả được gắn với chỉ tiêu này
            items = await AsyncExecuter.ToListAsync(
                query
                    .WhereActive()
                    .Where(x => resultValueIdsForIndicator.Contains(x.Id))
                    .OrderBy(x => x.Name)
            );
        }
        else
        {
            // Trả về tất cả kết quả không có trong junction table (generic results)
            var allLinkedResultValueIds = junctionQuery
                .Select(j => j.TestResultValueId)
                .Distinct()
                .ToList();

            items = await AsyncExecuter.ToListAsync(
                query
                    .WhereActive()
                    .Where(x => !allLinkedResultValueIds.Contains(x.Id))
                    .OrderBy(x => x.Name)
            );
        }

        return new ListResultDto<TestResultValueDto>(
            ObjectMapper.Map<List<TestResultValue>, List<TestResultValueDto>>(items)
        );
    }

    #region Indicator Management

    public async Task<ListResultDto<TestResultValueDto>> GetWithIndicatorsAsync()
    {
        var resultValues = await _repository.GetListAsync();
        var junctions = await _junctionRepository.GetListAsync();
        var indicators = await _indicatorRepository.GetListAsync();
        var specimenTypes = await _specimenTypeRepository.GetListAsync();

        var result = resultValues
            .OrderByDescending(x => x.CreationTime)
            .Select(rv =>
            {
                var dto = ObjectMapper.Map<TestResultValue, TestResultValueDto>(rv);

                var rvIndicators = junctions
                    .Where(j => j.TestResultValueId == rv.Id)
                    .OrderBy(j => j.SortOrder)
                    .Select(j =>
                    {
                        var indicator = indicators.FirstOrDefault(i => i.Id == j.IndicatorId);
                        var specimenType = specimenTypes.FirstOrDefault(st => st.Id == j.SpecimenTypeId);

                        return new TestResultValueIndicatorDto
                        {
                            Id = j.Id,
                            TestResultValueId = j.TestResultValueId,
                            IndicatorId = j.IndicatorId,
                            IndicatorCode = indicator?.Code ?? "",
                            IndicatorName = indicator?.Name ?? "",
                            IndicatorFormattedName = indicator?.FormattedName,
                            SpecimenTypeId = j.SpecimenTypeId,
                            SpecimenTypeCode = specimenType?.Code ?? "",
                            SpecimenTypeName = specimenType?.Name ?? "",
                            IsActive = j.IsActive,
                            SortOrder = j.SortOrder
                        };
                    })
                    .ToList();

                dto.Indicators = rvIndicators;
                dto.IndicatorCount = rvIndicators.Count;

                return dto;
            })
            .ToList();

        return new ListResultDto<TestResultValueDto>(result);
    }

    public async Task AddIndicatorsAsync(Guid testResultValueId, AddIndicatorsToTestResultValueDto input)
    {
        // Verify test result value exists
        var resultValue = await _repository.GetAsync(testResultValueId);

        // Get existing composite keys to avoid duplicates
        var existingQuery = await _junctionRepository.GetQueryableAsync();
        var existingKeys = existingQuery
            .Where(j => j.TestResultValueId == testResultValueId)
            .Select(j => new { j.IndicatorId, j.SpecimenTypeId })
            .ToList()
            .Select(x => (x.IndicatorId, x.SpecimenTypeId))
            .ToHashSet();

        // Get max sort order
        var existingSortOrders = existingQuery
            .Where(j => j.TestResultValueId == testResultValueId)
            .Select(j => j.SortOrder)
            .ToList();
        var maxSortOrder = existingSortOrders.Count > 0 ? existingSortOrders.Max() : 0;

        var newItems = new List<TestResultValueIndicator>();
        var sortOrder = maxSortOrder;

        foreach (var item in input.Indicators)
        {
            var key = (item.IndicatorId, item.SpecimenTypeId);

            // Skip if already exists
            if (existingKeys.Contains(key))
            {
                continue;
            }

            sortOrder++;
            var entity = new TestResultValueIndicator(
                GuidGenerator.Create(),
                testResultValueId,
                item.IndicatorId,
                item.SpecimenTypeId
            )
            {
                SortOrder = sortOrder
            };

            newItems.Add(entity);
            existingKeys.Add(key);
        }

        // Insert only new items
        if (newItems.Any())
        {
            await _junctionRepository.InsertManyAsync(newItems);
        }
    }

    public async Task RemoveIndicatorAsync(Guid testResultValueId, Guid indicatorId, Guid specimenTypeId)
    {
        var query = await _junctionRepository.GetQueryableAsync();
        var entity = query.FirstOrDefault(j =>
            j.TestResultValueId == testResultValueId &&
            j.IndicatorId == indicatorId &&
            j.SpecimenTypeId == specimenTypeId);

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("TestResultValueIndicator");
        }

        await _junctionRepository.DeleteAsync(entity);
    }

    public async Task<TestResultValueIndicatorDto> ToggleIndicatorActiveAsync(Guid testResultValueId, Guid indicatorId, Guid specimenTypeId)
    {
        var query = await _junctionRepository.GetQueryableAsync();
        var entity = query.FirstOrDefault(j =>
            j.TestResultValueId == testResultValueId &&
            j.IndicatorId == indicatorId &&
            j.SpecimenTypeId == specimenTypeId);

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("TestResultValueIndicator");
        }

        entity.IsActive = !entity.IsActive;
        await _junctionRepository.UpdateAsync(entity);

        // Get indicator and specimen type info for response
        var indicator = await _indicatorRepository.FindAsync(entity.IndicatorId);
        var specimenType = await _specimenTypeRepository.FindAsync(entity.SpecimenTypeId);

        return new TestResultValueIndicatorDto
        {
            Id = entity.Id,
            TestResultValueId = entity.TestResultValueId,
            IndicatorId = entity.IndicatorId,
            IndicatorCode = indicator?.Code ?? "",
            IndicatorName = indicator?.Name ?? "",
            IndicatorFormattedName = indicator?.FormattedName,
            SpecimenTypeId = entity.SpecimenTypeId,
            SpecimenTypeCode = specimenType?.Code ?? "",
            SpecimenTypeName = specimenType?.Name ?? "",
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder
        };
    }

    public async Task<int> BatchRemoveIndicatorsAsync(Guid testResultValueId, BatchRemoveIndicatorsDto input)
    {
        if (input.Keys == null || input.Keys.Count == 0)
        {
            return 0;
        }

        var query = await _junctionRepository.GetQueryableAsync();
        var keysSet = input.Keys
            .Select(k => (k.IndicatorId, k.SpecimenTypeId))
            .ToHashSet();

        var entities = query
            .Where(j => j.TestResultValueId == testResultValueId)
            .ToList()
            .Where(j => keysSet.Contains((j.IndicatorId, j.SpecimenTypeId)))
            .ToList();

        if (entities.Count == 0)
        {
            return 0;
        }

        await _junctionRepository.DeleteManyAsync(entities);
        return entities.Count;
    }

    #endregion
}
