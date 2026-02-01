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
    private readonly IDataFilter _dataFilter;

    public TestMethodAppService(
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IDataFilter dataFilter)
    {
        _testMethodRepository = testMethodRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _dataFilter = dataFilter;
    }

    public async Task<PagedResultDto<TestMethodDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await _testMethodRepository.GetQueryableAsync();
        var indicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
        );

        var dtos = items.Select(item =>
        {
            var dto = ObjectMapper.Map<TestMethod, TestMethodDto>(item);
            dto.IndicatorCount = indicatorQuery.Count(i => i.TestMethodId == item.Id);
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
            FormattedName = input.FormattedName
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
        var indicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();

        var items = await AsyncExecuter.ToListAsync(
            query
                .WhereActive()
                .OrderByThenBy(x => x.SortOrder, x => x.Name)
        );

        var dtos = items.Select(item =>
        {
            var dto = ObjectMapper.Map<TestMethod, TestMethodDto>(item);
            dto.IndicatorCount = indicatorQuery.Count(i => i.TestMethodId == item.Id);
            return dto;
        }).ToList();

        return new ListResultDto<TestMethodDto>(dtos);
    }

    public async Task<List<TestMethodWithIndicatorsDto>> GetAllWithIndicatorsAsync()
    {
        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodIndicators = await _testMethodIndicatorRepository.GetListAsync();
        var indicators = await _indicatorRepository.GetListAsync();

        var result = testMethods
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(tm =>
            {
                var dto = ObjectMapper.Map<TestMethod, TestMethodWithIndicatorsDto>(tm);

                var tmIndicatorList = new List<TestMethodIndicatorDto>();

                var parentIndicators = testMethodIndicators
                    .Where(tmi => tmi.TestMethodId == tm.Id)
                    .OrderBy(tmi => tmi.SortOrder)
                    .ToList();

                foreach (var tmi in parentIndicators)
                {
                    var indicator = indicators.FirstOrDefault(i => i.Id == tmi.IndicatorId);

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
                        IsActive = indicator?.IsActive ?? false,
                        SortOrder = tmi.SortOrder,
                        ParentIndicatorId = indicator?.ParentIndicatorId
                    });

                    // Add sub-indicators of this parent (from Indicator table, not stored in TestMethodIndicator)
                    var subIndicators = indicators
                        .Where(i => i.ParentIndicatorId == tmi.IndicatorId && i.IsActive)
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
                            ParentIndicatorId = subInd.ParentIndicatorId
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

        var tmIndicatorList = new List<TestMethodIndicatorDto>();

        foreach (var tmi in tmIndicators)
        {
            var indicator = allIndicators.FirstOrDefault(i => i.Id == tmi.IndicatorId);

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
                IsActive = indicator?.IsActive ?? false,
                SortOrder = tmi.SortOrder,
                ParentIndicatorId = indicator?.ParentIndicatorId
            });

            // Add sub-indicators of this parent
            var subIndicators = allIndicators
                .Where(i => i.ParentIndicatorId == tmi.IndicatorId && i.IsActive)
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
                    ParentIndicatorId = subInd.ParentIndicatorId
                });
            }
        }

        dto.Indicators = tmIndicatorList;
        dto.IndicatorCount = tmIndicators.Count; // Chỉ đếm parent indicators

        return dto;
    }

    public async Task<List<TestMethodIndicatorDto>> GetIndicatorsAsync(Guid testMethodId)
    {
        var testMethodIndicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();

        var tmIndicators = await AsyncExecuter.ToListAsync(
            testMethodIndicatorQuery.Where(tmi => tmi.TestMethodId == testMethodId).OrderBy(tmi => tmi.SortOrder)
        );

        // Get all indicators including potential sub-indicators
        var allIndicators = await AsyncExecuter.ToListAsync(indicatorQuery);

        var result = new List<TestMethodIndicatorDto>();

        foreach (var tmi in tmIndicators)
        {
            var indicator = allIndicators.FirstOrDefault(i => i.Id == tmi.IndicatorId);

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
                IsActive = indicator?.IsActive ?? false,
                SortOrder = tmi.SortOrder,
                ParentIndicatorId = indicator?.ParentIndicatorId
            });

            // Add sub-indicators of this parent
            var subIndicators = allIndicators
                .Where(i => i.ParentIndicatorId == tmi.IndicatorId && i.IsActive)
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
                    ParentIndicatorId = subInd.ParentIndicatorId
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
        var existingIndicatorIds = existingQuery
            .Where(tmi => tmi.TestMethodId == testMethodId)
            .Select(tmi => tmi.IndicatorId)
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
            // Skip if already exists
            if (existingIndicatorIds.Contains(item.IndicatorId))
            {
                continue;
            }

            sortOrder++;
            var entity = new TestMethodIndicator(
                GuidGenerator.Create(),
                testMethodId,
                item.IndicatorId,
                item.UnitPrice
            )
            {
                SortOrder = sortOrder
            };

            newItems.Add(entity);
            existingIndicatorIds.Add(item.IndicatorId);
        }

        if (newItems.Any())
        {
            await _testMethodIndicatorRepository.InsertManyAsync(newItems);
        }
    }

    public async Task RemoveIndicatorAsync(Guid testMethodId, Guid indicatorId)
    {
        var query = await _testMethodIndicatorRepository.GetQueryableAsync();
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(tmi => tmi.TestMethodId == testMethodId && tmi.IndicatorId == indicatorId)
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

    public async Task<int> RemoveIndicatorsBatchAsync(Guid testMethodId, List<Guid> indicatorIds)
    {
        if (indicatorIds == null || indicatorIds.Count == 0)
        {
            return 0;
        }

        // Find all sub-indicators of the parent indicators being removed
        // so we can also remove them from this test method
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var subIndicatorIds = indicatorQuery
            .Where(ind => ind.ParentIndicatorId.HasValue && indicatorIds.Contains(ind.ParentIndicatorId.Value))
            .Select(ind => ind.Id)
            .ToList();

        // Combine parent indicator IDs with their sub-indicator IDs
        var allIndicatorIdsToRemove = indicatorIds.Concat(subIndicatorIds).Distinct().ToList();

        var query = await _testMethodIndicatorRepository.GetQueryableAsync();
        var entities = query
            .Where(tmi => tmi.TestMethodId == testMethodId && allIndicatorIdsToRemove.Contains(tmi.IndicatorId))
            .ToList();

        if (entities.Count == 0)
        {
            return 0;
        }

        await _testMethodIndicatorRepository.DeleteManyAsync(entities);
        return entities.Count;
    }
}
