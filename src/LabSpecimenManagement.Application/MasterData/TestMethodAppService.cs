using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.MasterData.Indicators;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class TestMethodAppService : ApplicationService, ITestMethodAppService
{
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IDataFilter _dataFilter;

    public TestMethodAppService(
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IDataFilter dataFilter)
    {
        _testMethodRepository = testMethodRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _dataFilter = dataFilter;
    }

    public async Task<PagedResultDto<TestMethodDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await _testMethodRepository.GetQueryableAsync();

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .OrderByDescending(x => x.CreationTime)
                .ThenBy(x => x.Name)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
        );

        var countByMethod = await GetIndicatorCountByMethodAsync();

        var dtos = items.Select(item =>
        {
            var dto = ObjectMapper.Map<TestMethod, TestMethodDto>(item);
            dto.IndicatorCount = countByMethod.GetValueOrDefault(item.Id);
            return dto;
        }).ToList();

        return new PagedResultDto<TestMethodDto>(totalCount, dtos);
    }

    public async Task<TestMethodDto> GetAsync(Guid id)
    {
        var entity = await _testMethodRepository.GetAsync(id);
        var indicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();

        var dto = ObjectMapper.Map<TestMethod, TestMethodDto>(entity);
        dto.IndicatorCount = indicatorQuery.Count(i => i.TestMethodId == id);

        return dto;
    }

    public async Task<TestMethodDto> CreateAsync(CreateTestMethodDto input)
    {
        var code = await GenerateCodeAsync();

        var entity = new TestMethod(
            GuidGenerator.Create(),
            code,
            input.Name,
            input.Unit
        )
        {
            IsActive = input.IsActive,
            FormattedName = input.FormattedName,
            FormattedUnit = input.FormattedUnit
        };

        await _testMethodRepository.InsertAsync(entity);

        return ObjectMapper.Map<TestMethod, TestMethodDto>(entity);
    }

    public async Task<TestMethodDto> UpdateAsync(Guid id, UpdateTestMethodDto input)
    {
        var entity = await _testMethodRepository.GetAsync(id);

        entity.Name = input.Name;
        entity.FormattedName = input.FormattedName;
        entity.Unit = input.Unit;
        entity.FormattedUnit = input.FormattedUnit;
        entity.IsActive = input.IsActive;

        await _testMethodRepository.UpdateAsync(entity);

        var indicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var dto = ObjectMapper.Map<TestMethod, TestMethodDto>(entity);
        dto.IndicatorCount = indicatorQuery.Count(i => i.TestMethodId == id);

        return dto;
    }

    public async Task DeleteAsync(Guid id)
    {
        // Check if there are any indicators linked
        var indicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var hasIndicators = indicatorQuery.Any(i => i.TestMethodId == id);

        if (hasIndicators)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Không thể xóa phương pháp đang có chỉ tiêu. Vui lòng xóa hết chỉ tiêu trước.",
                "id");
        }

        await _testMethodRepository.DeleteAsync(id);
    }

    public async Task<ListResultDto<TestMethodDto>> GetActiveAsync()
    {
        var query = await _testMethodRepository.GetQueryableAsync();

        var items = await AsyncExecuter.ToListAsync(
            query
                .WhereActive()
                .OrderByThenBy(x => x.SortOrder, x => x.Name)
        );

        var countByMethod = await GetIndicatorCountByMethodAsync();

        var dtos = items.Select(item =>
        {
            var dto = ObjectMapper.Map<TestMethod, TestMethodDto>(item);
            dto.IndicatorCount = countByMethod.GetValueOrDefault(item.Id);
            return dto;
        }).ToList();

        return new ListResultDto<TestMethodDto>(dtos);
    }

    public async Task<ListResultDto<TestMethodDto>> GetAllAsync()
    {
        var query = await _testMethodRepository.GetQueryableAsync();

        var items = await AsyncExecuter.ToListAsync(
            query
                .OrderByThenBy(x => x.SortOrder, x => x.Name)
        );

        var countByMethod = await GetIndicatorCountByMethodAsync();

        var dtos = items.Select(item =>
        {
            var dto = ObjectMapper.Map<TestMethod, TestMethodDto>(item);
            dto.IndicatorCount = countByMethod.GetValueOrDefault(item.Id);
            return dto;
        }).ToList();

        return new ListResultDto<TestMethodDto>(dtos);
    }

    // 1 câu GroupBy thay cho N câu COUNT chạy đồng bộ trong vòng lặp map DTO
    private async Task<Dictionary<Guid, int>> GetIndicatorCountByMethodAsync()
    {
        var indicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var counts = await AsyncExecuter.ToListAsync(
            indicatorQuery
                .GroupBy(i => i.TestMethodId)
                .Select(g => new { g.Key, Count = g.Count() })
        );
        return counts.ToDictionary(x => x.Key, x => x.Count);
    }

    public async Task<List<TestMethodWithIndicatorsDto>> GetAllWithIndicatorsAsync()
    {
        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodIndicators = await _testMethodIndicatorRepository.GetListAsync();
        var indicators = await _indicatorRepository.GetListAsync();
        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeDict = specimenTypes.ToDictionary(st => st.Id);

        // Index 1 lần thay cho quét tuyến tính trong vòng lặp (O(N*M) -> O(N));
        // ToLookup giữ nguyên thứ tự nguồn nên kết quả sort không đổi
        var indicatorById = indicators.ToDictionary(i => i.Id);
        var tmIndicatorsByMethod = testMethodIndicators.ToLookup(tmi => tmi.TestMethodId);
        var subIndicatorsByParent = indicators
            .Where(i => i.ParentIndicatorId.HasValue)
            .ToLookup(i => i.ParentIndicatorId!.Value);

        var result = testMethods
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(tm =>
            {
                var dto = ObjectMapper.Map<TestMethod, TestMethodWithIndicatorsDto>(tm);

                var tmIndicatorList = new List<TestMethodIndicatorDto>();

                var parentIndicators = tmIndicatorsByMethod[tm.Id]
                    .OrderBy(tmi => tmi.SortOrder)
                    .ToList();

                foreach (var tmi in parentIndicators)
                {
                    var indicator = indicatorById.GetValueOrDefault(tmi.IndicatorId);
                    var specimenType = tmi.SpecimenTypeId.HasValue && specimenTypeDict.TryGetValue(tmi.SpecimenTypeId.Value, out var st) ? st : null;

                    var effectiveUnit = tmi.Unit ?? tm.Unit;
                    var effectiveFormattedUnit = tmi.FormattedUnit ?? tm.FormattedUnit;

                    // Add parent indicator
                    tmIndicatorList.Add(new TestMethodIndicatorDto
                    {
                        Id = tmi.Id,
                        TestMethodId = tmi.TestMethodId,
                        IndicatorId = tmi.IndicatorId,
                        IndicatorCode = indicator?.Code ?? "",
                        IndicatorName = indicator?.Name ?? "",
                        IndicatorFormattedName = indicator?.FormattedName,
                        UnitPrice = tmi.UnitPrice,
                        IsActive = tmi.IsActive,
                        SortOrder = tmi.SortOrder,
                        ParentIndicatorId = indicator?.ParentIndicatorId,
                        SpecimenTypeId = tmi.SpecimenTypeId,
                        SpecimenTypeCode = specimenType?.Code,
                        SpecimenTypeName = specimenType?.Name,
                        Unit = effectiveUnit,
                        FormattedUnit = effectiveFormattedUnit,
                        IsSubcontract = tmi.IsSubcontract
                    });

                    // Add sub-indicators of this parent (from Indicator table, not stored in TestMethodIndicator)
                    var subIndicators = subIndicatorsByParent[tmi.IndicatorId]
                        .OrderBy(i => i.SortOrder)
                        .ToList();

                    foreach (var subInd in subIndicators)
                    {
                        tmIndicatorList.Add(new TestMethodIndicatorDto
                        {
                            Id = Guid.Empty, // Sub-indicator không có record riêng trong TestMethodIndicator
                            TestMethodId = tmi.TestMethodId,
                            IndicatorId = subInd.Id,
                            IndicatorCode = subInd.Code,
                            IndicatorName = subInd.Name,
                            IndicatorFormattedName = subInd.FormattedName,
                            UnitPrice = 0, // Sub-indicator theo giá cha, không có giá riêng
                            IsActive = subInd.IsActive,
                            SortOrder = tmi.SortOrder, // Giữ sortOrder của parent để group đúng
                            ParentIndicatorId = subInd.ParentIndicatorId,
                            // Sub-indicator inherit SpecimenTypeId từ parent để group đúng
                            SpecimenTypeId = tmi.SpecimenTypeId,
                            SpecimenTypeCode = specimenType?.Code,
                            SpecimenTypeName = specimenType?.Name,
                            Unit = effectiveUnit,
                            FormattedUnit = effectiveFormattedUnit,
                            IsSubcontract = tmi.IsSubcontract
                        });
                    }
                }

                dto.Indicators = tmIndicatorList;
                dto.IndicatorCount = parentIndicators.Count; // Chỉ đếm parent indicators

                return dto;
            })
            .ToList();

        return result;
    }

    public async Task<TestMethodWithIndicatorsDto> GetWithIndicatorsAsync(Guid id)
    {
        var testMethod = await _testMethodRepository.GetAsync(id);
        var testMethodIndicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();

        var dto = ObjectMapper.Map<TestMethod, TestMethodWithIndicatorsDto>(testMethod);

        var tmIndicators = await AsyncExecuter.ToListAsync(
            testMethodIndicatorQuery.Where(tmi => tmi.TestMethodId == id).OrderBy(tmi => tmi.SortOrder)
        );

        // Get all indicators including potential sub-indicators
        var allIndicators = await AsyncExecuter.ToListAsync(indicatorQuery);

        // Get specimen types for enrichment
        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeDict = specimenTypes.ToDictionary(st => st.Id);

        var tmIndicatorList = new List<TestMethodIndicatorDto>();

        foreach (var tmi in tmIndicators)
        {
            var indicator = allIndicators.FirstOrDefault(i => i.Id == tmi.IndicatorId);
            var specimenType = tmi.SpecimenTypeId.HasValue && specimenTypeDict.TryGetValue(tmi.SpecimenTypeId.Value, out var st) ? st : null;
            var effectiveUnit = tmi.Unit ?? testMethod.Unit;
            var effectiveFormattedUnit = tmi.FormattedUnit ?? testMethod.FormattedUnit;

            // Add parent indicator
            tmIndicatorList.Add(new TestMethodIndicatorDto
            {
                Id = tmi.Id,
                TestMethodId = tmi.TestMethodId,
                IndicatorId = tmi.IndicatorId,
                IndicatorCode = indicator?.Code ?? "",
                IndicatorName = indicator?.Name ?? "",
                IndicatorFormattedName = indicator?.FormattedName,
                UnitPrice = tmi.UnitPrice,
                IsActive = tmi.IsActive,
                SortOrder = tmi.SortOrder,
                ParentIndicatorId = indicator?.ParentIndicatorId,
                SpecimenTypeId = tmi.SpecimenTypeId,
                SpecimenTypeCode = specimenType?.Code,
                SpecimenTypeName = specimenType?.Name,
                Unit = effectiveUnit,
                FormattedUnit = effectiveFormattedUnit,
                IsSubcontract = tmi.IsSubcontract
            });

            // Add sub-indicators of this parent
            var subIndicators = allIndicators
                .Where(i => i.ParentIndicatorId == tmi.IndicatorId)
                .OrderBy(i => i.SortOrder)
                .ToList();

            foreach (var subInd in subIndicators)
            {
                tmIndicatorList.Add(new TestMethodIndicatorDto
                {
                    Id = Guid.Empty,
                    TestMethodId = tmi.TestMethodId,
                    IndicatorId = subInd.Id,
                    IndicatorCode = subInd.Code,
                    IndicatorName = subInd.Name,
                    IndicatorFormattedName = subInd.FormattedName,
                    UnitPrice = 0,
                    IsActive = subInd.IsActive,
                    SortOrder = tmi.SortOrder,
                    ParentIndicatorId = subInd.ParentIndicatorId,
                    SpecimenTypeId = tmi.SpecimenTypeId,
                    SpecimenTypeCode = specimenType?.Code,
                    SpecimenTypeName = specimenType?.Name,
                    Unit = effectiveUnit,
                    FormattedUnit = effectiveFormattedUnit,
                    IsSubcontract = tmi.IsSubcontract
                });
            }
        }

        dto.Indicators = tmIndicatorList;
        dto.IndicatorCount = tmIndicators.Count; // Chỉ đếm parent indicators

        return dto;
    }

    public async Task<List<TestMethodIndicatorDto>> GetIndicatorsAsync(Guid testMethodId)
    {
        var testMethod = await _testMethodRepository.GetAsync(testMethodId);
        var testMethodIndicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();

        var tmIndicators = await AsyncExecuter.ToListAsync(
            testMethodIndicatorQuery.Where(tmi => tmi.TestMethodId == testMethodId).OrderBy(tmi => tmi.SortOrder)
        );

        // Get all indicators including potential sub-indicators
        var allIndicators = await AsyncExecuter.ToListAsync(indicatorQuery);

        // Get specimen types for enrichment
        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeDict = specimenTypes.ToDictionary(st => st.Id);

        var result = new List<TestMethodIndicatorDto>();

        foreach (var tmi in tmIndicators)
        {
            var indicator = allIndicators.FirstOrDefault(i => i.Id == tmi.IndicatorId);
            var specimenType = tmi.SpecimenTypeId.HasValue && specimenTypeDict.TryGetValue(tmi.SpecimenTypeId.Value, out var st) ? st : null;
            var effectiveUnit = tmi.Unit ?? testMethod.Unit;
            var effectiveFormattedUnit = tmi.FormattedUnit ?? testMethod.FormattedUnit;

            // Add parent indicator
            result.Add(new TestMethodIndicatorDto
            {
                Id = tmi.Id,
                TestMethodId = tmi.TestMethodId,
                IndicatorId = tmi.IndicatorId,
                IndicatorCode = indicator?.Code ?? "",
                IndicatorName = indicator?.Name ?? "",
                IndicatorFormattedName = indicator?.FormattedName,
                UnitPrice = tmi.UnitPrice,
                IsActive = tmi.IsActive,
                SortOrder = tmi.SortOrder,
                ParentIndicatorId = indicator?.ParentIndicatorId,
                SpecimenTypeId = tmi.SpecimenTypeId,
                SpecimenTypeCode = specimenType?.Code,
                SpecimenTypeName = specimenType?.Name,
                Unit = effectiveUnit,
                FormattedUnit = effectiveFormattedUnit
            });

            // Add sub-indicators of this parent
            var subIndicators = allIndicators
                .Where(i => i.ParentIndicatorId == tmi.IndicatorId)
                .OrderBy(i => i.SortOrder)
                .ToList();

            foreach (var subInd in subIndicators)
            {
                result.Add(new TestMethodIndicatorDto
                {
                    Id = Guid.Empty,
                    TestMethodId = tmi.TestMethodId,
                    IndicatorId = subInd.Id,
                    IndicatorCode = subInd.Code,
                    IndicatorName = subInd.Name,
                    IndicatorFormattedName = subInd.FormattedName,
                    UnitPrice = 0,
                    IsActive = subInd.IsActive,
                    SortOrder = tmi.SortOrder,
                    ParentIndicatorId = subInd.ParentIndicatorId,
                    SpecimenTypeId = tmi.SpecimenTypeId,
                    SpecimenTypeCode = specimenType?.Code,
                    SpecimenTypeName = specimenType?.Name,
                    Unit = effectiveUnit,
                    FormattedUnit = effectiveFormattedUnit
                });
            }
        }

        return result;
    }

    public async Task AddIndicatorsAsync(Guid testMethodId, AddIndicatorsToTestMethodDto input)
    {
        // Verify test method exists
        var testMethod = await _testMethodRepository.GetAsync(testMethodId);

        var existingQuery = await _testMethodIndicatorRepository.GetQueryableAsync();

        // Check duplicate by (IndicatorId, SpecimenTypeId) - same indicator can be added multiple times with different specimen types
        var existingKeys = existingQuery
            .Where(tmi => tmi.TestMethodId == testMethodId)
            .Select(tmi => new { tmi.IndicatorId, tmi.SpecimenTypeId })
            .ToList()
            .Select(x => (x.IndicatorId, x.SpecimenTypeId))
            .ToHashSet();

        var existingSortOrders = await AsyncExecuter.ToListAsync(
            existingQuery
                .Where(tmi => tmi.TestMethodId == testMethodId)
                .Select(tmi => tmi.SortOrder)
        );
        var maxSortOrder = existingSortOrders.Count > 0 ? existingSortOrders.Max() : 0;

        var newItems = new List<TestMethodIndicator>();
        var sortOrder = maxSortOrder;

        foreach (var item in input.Indicators)
        {
            // Skip if already exists (same indicator + same specimen type)
            var key = (item.IndicatorId, item.SpecimenTypeId);
            if (existingKeys.Contains(key))
            {
                continue;
            }

            sortOrder++;
            var entity = new TestMethodIndicator(
                GuidGenerator.Create(),
                testMethodId,
                item.IndicatorId,
                item.UnitPrice,
                item.SpecimenTypeId,
                item.Unit ?? testMethod.Unit,
                item.FormattedUnit ?? testMethod.FormattedUnit
            )
            {
                SortOrder = sortOrder,
                IsSubcontract = item.IsSubcontract
            };

            newItems.Add(entity);
            existingKeys.Add(key);
        }

        if (newItems.Any())
        {
            await _testMethodIndicatorRepository.InsertManyAsync(newItems);

            if (testMethod.Name != "-")
            {
                var indicatorIds = newItems.Select(x => x.IndicatorId).Distinct().ToList();
                var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
                var indicatorsToUpdate = indicatorQuery
                    .Where(i => indicatorIds.Contains(i.Id) && i.NeedsMethodSetup)
                    .ToList();

                foreach (var ind in indicatorsToUpdate)
                {
                    ind.NeedsMethodSetup = false;
                    await _indicatorRepository.UpdateAsync(ind);
                }
            }
        }
    }

    public async Task<TestMethodIndicatorDto> UpdateIndicatorAsync(Guid testMethodId, Guid indicatorId, Guid? specimenTypeId, UpdateTestMethodIndicatorDto input)
    {
        var query = await _testMethodIndicatorRepository.GetQueryableAsync();

        // Find by composite key (TestMethodId + IndicatorId + SpecimenTypeId)
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(tmi => tmi.TestMethodId == testMethodId
                && tmi.IndicatorId == indicatorId
                && tmi.SpecimenTypeId == specimenTypeId)
        );

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("TestMethodIndicator");
        }

        // Determine target TestMethodId and SpecimenTypeId (if changing)
        var targetTestMethodId = input.NewTestMethodId ?? testMethodId;
        var targetSpecimenTypeId = input.NewSpecimenTypeId.HasValue
            ? (input.NewSpecimenTypeId.Value == Guid.Empty ? (Guid?)null : input.NewSpecimenTypeId.Value)
            : specimenTypeId;
        var isChangingKey = targetTestMethodId != testMethodId || targetSpecimenTypeId != specimenTypeId;

        if (isChangingKey)
        {
            // Check unique constraint: (targetTestMethodId, indicatorId, targetSpecimenTypeId) must not exist
            var duplicate = await AsyncExecuter.FirstOrDefaultAsync(
                query.Where(tmi => tmi.TestMethodId == targetTestMethodId
                    && tmi.IndicatorId == indicatorId
                    && tmi.SpecimenTypeId == targetSpecimenTypeId
                    && tmi.Id != entity.Id)
            );

            if (duplicate != null)
            {
                var indicator2 = await _indicatorRepository.FindAsync(indicatorId);
                var specimenType2 = targetSpecimenTypeId.HasValue
                    ? await _specimenTypeRepository.FindAsync(targetSpecimenTypeId.Value)
                    : null;
                var indicatorLabel = indicator2?.Name ?? "chỉ tiêu";
                var specimenLabel = specimenType2?.Name ?? "nền mẫu";
                throw new Volo.Abp.UserFriendlyException($"Đã tồn tại phương pháp cho {indicatorLabel} của {specimenLabel}");
            }

            entity.TestMethodId = targetTestMethodId;
            entity.SpecimenTypeId = targetSpecimenTypeId;
        }

        // Update fields
        entity.UnitPrice = input.UnitPrice;
        entity.Unit = input.Unit;
        entity.FormattedUnit = input.FormattedUnit;
        entity.IsSubcontract = input.IsSubcontract;

        await _testMethodIndicatorRepository.UpdateAsync(entity);

        // Get indicator and test method info for response
        var actualTestMethodId = entity.TestMethodId;
        var actualSpecimenTypeId = entity.SpecimenTypeId;
        var indicator = await _indicatorRepository.FindAsync(indicatorId);
        var testMethod = await _testMethodRepository.FindAsync(actualTestMethodId);

        if (indicator is { NeedsMethodSetup: true } && testMethod?.Name != "-")
        {
            indicator.NeedsMethodSetup = false;
            await _indicatorRepository.UpdateAsync(indicator);
        }
        var specimenType = actualSpecimenTypeId.HasValue
            ? await _specimenTypeRepository.FindAsync(actualSpecimenTypeId.Value)
            : null;

        return new TestMethodIndicatorDto
        {
            Id = entity.Id,
            TestMethodId = entity.TestMethodId,
            IndicatorId = entity.IndicatorId,
            IndicatorCode = indicator?.Code ?? "",
            IndicatorName = indicator?.Name ?? "",
            IndicatorFormattedName = indicator?.FormattedName,
            UnitPrice = entity.UnitPrice,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder,
            ParentIndicatorId = indicator?.ParentIndicatorId,
            SpecimenTypeId = entity.SpecimenTypeId,
            SpecimenTypeCode = specimenType?.Code,
            SpecimenTypeName = specimenType?.Name,
            Unit = entity.Unit ?? testMethod?.Unit,
            FormattedUnit = entity.FormattedUnit ?? testMethod?.FormattedUnit,
            IsSubcontract = entity.IsSubcontract
        };
    }

    public async Task ToggleIndicatorActiveAsync(Guid testMethodId, Guid indicatorId, Guid? specimenTypeId)
    {
        var query = await _testMethodIndicatorRepository.GetQueryableAsync();
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(tmi => tmi.TestMethodId == testMethodId
                && tmi.IndicatorId == indicatorId
                && tmi.SpecimenTypeId == specimenTypeId)
        );

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("TestMethodIndicator");
        }

        entity.IsActive = !entity.IsActive;
        await _testMethodIndicatorRepository.UpdateAsync(entity);
    }

    public async Task RemoveIndicatorAsync(Guid testMethodId, Guid indicatorId, Guid? specimenTypeId = null)
    {
        var query = await _testMethodIndicatorRepository.GetQueryableAsync();
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(tmi => tmi.TestMethodId == testMethodId
                && tmi.IndicatorId == indicatorId
                && tmi.SpecimenTypeId == specimenTypeId)
        );

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("TestMethodIndicator");
        }

        // Find all sub-indicators of this parent indicator
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var subIndicatorIds = indicatorQuery
            .Where(ind => ind.ParentIndicatorId == indicatorId)
            .Select(ind => ind.Id)
            .ToList();

        // Get TestMethodIndicator links for sub-indicators (if any)
        var subIndicatorLinks = query
            .Where(tmi => tmi.TestMethodId == testMethodId && subIndicatorIds.Contains(tmi.IndicatorId))
            .ToList();

        // Delete parent and all sub-indicator links
        var allToDelete = new List<TestMethodIndicator> { entity };
        allToDelete.AddRange(subIndicatorLinks);

        await _testMethodIndicatorRepository.DeleteManyAsync(allToDelete);
    }

    public async Task<TestMethodDto> ToggleActiveAsync(Guid id)
    {
        var entity = await _testMethodRepository.GetAsync(id);
        entity.IsActive = !entity.IsActive;
        await _testMethodRepository.UpdateAsync(entity);

        var indicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var dto = ObjectMapper.Map<TestMethod, TestMethodDto>(entity);
        dto.IndicatorCount = indicatorQuery.Count(i => i.TestMethodId == id);

        return dto;
    }

    private async Task<string> GenerateCodeAsync()
    {
        var today = DateTime.Now;
        var datePrefix = $"PP{today:ddMMyyyy}";

        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _testMethodRepository.GetQueryableAsync();
            var maxCode = query
                .Where(x => x.Code.StartsWith(datePrefix))
                .Select(x => x.Code)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            int nextNumber = 1;
            if (maxCode != null)
            {
                var numberPart = maxCode.Substring(datePrefix.Length);
                if (int.TryParse(numberPart, out var num))
                {
                    nextNumber = num + 1;
                }
            }

            if (nextNumber > 999)
            {
                throw new Common.Exceptions.InvalidOperationException(
                    "Đã đạt giới hạn số phương pháp trong ngày (999). Vui lòng thử lại vào ngày mai.");
            }

            return $"{datePrefix}{nextNumber:D3}";
        }
    }

    public async Task<int> RemoveIndicatorsBatchAsync(Guid testMethodId, List<IndicatorKeyDto> indicatorKeys)
    {
        if (indicatorKeys == null || indicatorKeys.Count == 0)
        {
            return 0;
        }

        // Extract indicator IDs for finding sub-indicators
        var parentIndicatorIds = indicatorKeys.Select(k => k.IndicatorId).Distinct().ToList();

        // Find all sub-indicators of the parent indicators being removed
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var subIndicatorIds = indicatorQuery
            .Where(ind => ind.ParentIndicatorId.HasValue && parentIndicatorIds.Contains(ind.ParentIndicatorId.Value))
            .Select(ind => ind.Id)
            .ToList();

        var query = await _testMethodIndicatorRepository.GetQueryableAsync();

        // Build a set of keys for efficient lookup (IndicatorId, SpecimenTypeId)
        var keysToRemove = indicatorKeys
            .Select(k => (k.IndicatorId, k.SpecimenTypeId))
            .ToHashSet();

        // Also add sub-indicator keys with the same specimen types as their parents
        foreach (var key in indicatorKeys)
        {
            foreach (var subId in subIndicatorIds)
            {
                // Check if this sub-indicator belongs to the current parent
                var parent = indicatorQuery.FirstOrDefault(i => i.Id == subId && i.ParentIndicatorId == key.IndicatorId);
                if (parent != null)
                {
                    keysToRemove.Add((subId, key.SpecimenTypeId));
                }
            }
        }

        // Find all matching TestMethodIndicator entities
        var allEntities = query
            .Where(tmi => tmi.TestMethodId == testMethodId)
            .ToList();

        var entitiesToDelete = allEntities
            .Where(tmi => keysToRemove.Contains((tmi.IndicatorId, tmi.SpecimenTypeId)))
            .ToList();

        if (entitiesToDelete.Count == 0)
        {
            return 0;
        }

        await _testMethodIndicatorRepository.DeleteManyAsync(entitiesToDelete);
        return entitiesToDelete.Count;
    }
}
