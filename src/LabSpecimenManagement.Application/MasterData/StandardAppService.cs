using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
public class StandardAppService : ApplicationService, IStandardAppService
{
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IDataFilter _dataFilter;

    public StandardAppService(
        IRepository<Standard, Guid> standardRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IDataFilter dataFilter)
    {
        _standardRepository = standardRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _dataFilter = dataFilter;
    }

    public async Task<PagedResultDto<StandardDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await _standardRepository.GetQueryableAsync();
        var indicatorQuery = await _standardIndicatorRepository.GetQueryableAsync();

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
            var dto = ObjectMapper.Map<Standard, StandardDto>(item);
            dto.IndicatorCount = indicatorQuery.Count(i => i.StandardId == item.Id);
            return dto;
        }).ToList();

        return new PagedResultDto<StandardDto>(totalCount, dtos);
    }

    public async Task<StandardDto> GetAsync(Guid id)
    {
        var entity = await _standardRepository.GetAsync(id);
        var indicatorQuery = await _standardIndicatorRepository.GetQueryableAsync();

        var dto = ObjectMapper.Map<Standard, StandardDto>(entity);
        dto.IndicatorCount = indicatorQuery.Count(i => i.StandardId == id);

        return dto;
    }

    public async Task<StandardDto> CreateAsync(CreateStandardDto input)
    {
        var code = await GenerateCodeAsync();

        var entity = new Standard(
            GuidGenerator.Create(),
            code,
            input.Name
        )
        {
            LimitDescription = input.LimitDescription,
            IsActive = input.IsActive
        };

        await _standardRepository.InsertAsync(entity);

        return ObjectMapper.Map<Standard, StandardDto>(entity);
    }

    public async Task<StandardDto> UpdateAsync(Guid id, UpdateStandardDto input)
    {
        var entity = await _standardRepository.GetAsync(id);

        entity.Name = input.Name;
        entity.LimitDescription = input.LimitDescription;
        entity.IsActive = input.IsActive;

        await _standardRepository.UpdateAsync(entity);

        var indicatorQuery = await _standardIndicatorRepository.GetQueryableAsync();
        var dto = ObjectMapper.Map<Standard, StandardDto>(entity);
        dto.IndicatorCount = indicatorQuery.Count(i => i.StandardId == id);

        return dto;
    }

    public async Task DeleteAsync(Guid id)
    {
        // Check if there are any indicators linked
        var indicatorQuery = await _standardIndicatorRepository.GetQueryableAsync();
        var hasIndicators = indicatorQuery.Any(i => i.StandardId == id);

        if (hasIndicators)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Không thể xóa tiêu chuẩn đang có chỉ tiêu. Vui lòng xóa hết chỉ tiêu trước.",
                "id");
        }

        await _standardRepository.DeleteAsync(id);
    }

    public async Task<ListResultDto<StandardDto>> GetActiveAsync()
    {
        var query = await _standardRepository.GetQueryableAsync();
        var indicatorQuery = await _standardIndicatorRepository.GetQueryableAsync();

        var items = await AsyncExecuter.ToListAsync(
            query
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
        );

        var dtos = items.Select(item =>
        {
            var dto = ObjectMapper.Map<Standard, StandardDto>(item);
            dto.IndicatorCount = indicatorQuery.Count(i => i.StandardId == item.Id);
            return dto;
        }).ToList();

        return new ListResultDto<StandardDto>(dtos);
    }

    public async Task<StandardDto> ToggleActiveAsync(Guid id)
    {
        var entity = await _standardRepository.GetAsync(id);
        entity.IsActive = !entity.IsActive;
        await _standardRepository.UpdateAsync(entity);

        var indicatorQuery = await _standardIndicatorRepository.GetQueryableAsync();
        var dto = ObjectMapper.Map<Standard, StandardDto>(entity);
        dto.IndicatorCount = indicatorQuery.Count(i => i.StandardId == id);

        return dto;
    }

    public async Task<List<StandardWithIndicatorsDto>> GetAllWithIndicatorsAsync()
    {
        var standards = await _standardRepository.GetListAsync();
        var standardIndicators = await _standardIndicatorRepository.GetListAsync();
        var indicators = await _indicatorRepository.GetListAsync();
        var testMethods = await _testMethodRepository.GetListAsync();

        var result = standards
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(s =>
            {
                var dto = ObjectMapper.Map<Standard, StandardWithIndicatorsDto>(s);

                var sIndicators = standardIndicators
                    .Where(si => si.StandardId == s.Id)
                    .OrderBy(si => si.SortOrder)
                    .Select(si =>
                    {
                        var indicator = indicators.FirstOrDefault(i => i.Id == si.IndicatorId);
                        var testMethod = testMethods.FirstOrDefault(tm => tm.Id == si.TestMethodId);

                        return new StandardIndicatorDto
                        {
                            Id = si.Id,
                            StandardId = si.StandardId,
                            IndicatorId = si.IndicatorId,
                            IndicatorCode = indicator?.Code ?? "",
                            IndicatorName = indicator?.Name ?? "",
                            TestMethodId = si.TestMethodId,
                            TestMethodCode = testMethod?.Code ?? "",
                            TestMethodName = testMethod?.Name ?? "",
                            Unit = si.Unit,
                            ComparisonOperator = si.ComparisonOperator,
                            AllowedLimit = si.AllowedLimit,
                            IsActive = si.IsActive,
                            SortOrder = si.SortOrder
                        };
                    })
                    .ToList();

                dto.Indicators = sIndicators;
                dto.IndicatorCount = sIndicators.Count;

                return dto;
            })
            .ToList();

        return result;
    }

    public async Task<StandardWithIndicatorsDto> GetWithIndicatorsAsync(Guid id)
    {
        var standard = await _standardRepository.GetAsync(id);
        var standardIndicatorQuery = await _standardIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();

        var dto = ObjectMapper.Map<Standard, StandardWithIndicatorsDto>(standard);

        var sIndicators = await AsyncExecuter.ToListAsync(
            standardIndicatorQuery.Where(si => si.StandardId == id).OrderBy(si => si.SortOrder)
        );

        var indicatorIds = sIndicators.Select(si => si.IndicatorId).ToList();
        var testMethodIds = sIndicators.Select(si => si.TestMethodId).ToList();

        var indicators = await AsyncExecuter.ToListAsync(
            indicatorQuery.Where(i => indicatorIds.Contains(i.Id))
        );
        var testMethods = await AsyncExecuter.ToListAsync(
            testMethodQuery.Where(tm => testMethodIds.Contains(tm.Id))
        );

        dto.Indicators = sIndicators.Select(si =>
        {
            var indicator = indicators.FirstOrDefault(i => i.Id == si.IndicatorId);
            var testMethod = testMethods.FirstOrDefault(tm => tm.Id == si.TestMethodId);

            return new StandardIndicatorDto
            {
                Id = si.Id,
                StandardId = si.StandardId,
                IndicatorId = si.IndicatorId,
                IndicatorCode = indicator?.Code ?? "",
                IndicatorName = indicator?.Name ?? "",
                TestMethodId = si.TestMethodId,
                TestMethodCode = testMethod?.Code ?? "",
                TestMethodName = testMethod?.Name ?? "",
                Unit = si.Unit,
                ComparisonOperator = si.ComparisonOperator,
                AllowedLimit = si.AllowedLimit,
                IsActive = si.IsActive,
                SortOrder = si.SortOrder
            };
        }).ToList();

        dto.IndicatorCount = dto.Indicators.Count;

        return dto;
    }

    public async Task<List<StandardIndicatorDto>> GetIndicatorsAsync(Guid standardId)
    {
        var standardIndicatorQuery = await _standardIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();

        var sIndicators = await AsyncExecuter.ToListAsync(
            standardIndicatorQuery.Where(si => si.StandardId == standardId).OrderBy(si => si.SortOrder)
        );

        var indicatorIds = sIndicators.Select(si => si.IndicatorId).ToList();
        var testMethodIds = sIndicators.Select(si => si.TestMethodId).ToList();

        var indicators = await AsyncExecuter.ToListAsync(
            indicatorQuery.Where(i => indicatorIds.Contains(i.Id))
        );
        var testMethods = await AsyncExecuter.ToListAsync(
            testMethodQuery.Where(tm => testMethodIds.Contains(tm.Id))
        );

        return sIndicators.Select(si =>
        {
            var indicator = indicators.FirstOrDefault(i => i.Id == si.IndicatorId);
            var testMethod = testMethods.FirstOrDefault(tm => tm.Id == si.TestMethodId);

            return new StandardIndicatorDto
            {
                Id = si.Id,
                StandardId = si.StandardId,
                IndicatorId = si.IndicatorId,
                IndicatorCode = indicator?.Code ?? "",
                IndicatorName = indicator?.Name ?? "",
                TestMethodId = si.TestMethodId,
                TestMethodCode = testMethod?.Code ?? "",
                TestMethodName = testMethod?.Name ?? "",
                Unit = si.Unit,
                ComparisonOperator = si.ComparisonOperator,
                AllowedLimit = si.AllowedLimit,
                IsActive = si.IsActive,
                SortOrder = si.SortOrder
            };
        }).ToList();
    }

    public async Task AddIndicatorsAsync(Guid standardId, AddIndicatorsToStandardDto input)
    {
        // Verify standard exists
        var standard = await _standardRepository.GetAsync(standardId);

        var existingQuery = await _standardIndicatorRepository.GetQueryableAsync();

        // Get max sort order
        var existingSortOrders = await AsyncExecuter.ToListAsync(
            existingQuery
                .Where(si => si.StandardId == standardId)
                .Select(si => si.SortOrder)
        );
        var maxSortOrder = existingSortOrders.Count > 0 ? existingSortOrders.Max() : 0;

        var newItems = new List<StandardIndicator>();
        var sortOrder = maxSortOrder;

        foreach (var item in input.Indicators)
        {
            // Check for duplicate (same standardId + indicatorId + testMethodId)
            var isDuplicate = existingQuery.Any(si =>
                si.StandardId == standardId &&
                si.IndicatorId == item.IndicatorId &&
                si.TestMethodId == item.TestMethodId);

            if (isDuplicate)
            {
                continue;
            }

            sortOrder++;
            var entity = new StandardIndicator(
                GuidGenerator.Create(),
                standardId,
                item.IndicatorId,
                item.TestMethodId,
                item.Unit
            )
            {
                ComparisonOperator = item.ComparisonOperator,
                AllowedLimit = item.AllowedLimit,
                SortOrder = sortOrder
            };

            newItems.Add(entity);
        }

        if (newItems.Any())
        {
            await _standardIndicatorRepository.InsertManyAsync(newItems);
        }
    }

    public async Task<StandardIndicatorDto> UpdateIndicatorAsync(Guid standardId, Guid id, UpdateStandardIndicatorDto input)
    {
        var query = await _standardIndicatorRepository.GetQueryableAsync();
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(si => si.StandardId == standardId && si.Id == id)
        );

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("StandardIndicator", id);
        }

        entity.ComparisonOperator = input.ComparisonOperator;
        entity.AllowedLimit = input.AllowedLimit;
        entity.IsActive = input.IsActive;

        await _standardIndicatorRepository.UpdateAsync(entity);

        // Get indicator and test method info for response
        var indicator = await _indicatorRepository.FindAsync(entity.IndicatorId);
        var testMethod = await _testMethodRepository.FindAsync(entity.TestMethodId);

        return new StandardIndicatorDto
        {
            Id = entity.Id,
            StandardId = entity.StandardId,
            IndicatorId = entity.IndicatorId,
            IndicatorCode = indicator?.Code ?? "",
            IndicatorName = indicator?.Name ?? "",
            TestMethodId = entity.TestMethodId,
            TestMethodCode = testMethod?.Code ?? "",
            TestMethodName = testMethod?.Name ?? "",
            Unit = entity.Unit,
            ComparisonOperator = entity.ComparisonOperator,
            AllowedLimit = entity.AllowedLimit,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder
        };
    }

    public async Task RemoveIndicatorAsync(Guid standardId, Guid id)
    {
        var query = await _standardIndicatorRepository.GetQueryableAsync();
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(si => si.StandardId == standardId && si.Id == id)
        );

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("StandardIndicator", id);
        }

        await _standardIndicatorRepository.DeleteAsync(entity);
    }

    public async Task<StandardIndicatorDto> ToggleIndicatorActiveAsync(Guid standardId, Guid id)
    {
        var query = await _standardIndicatorRepository.GetQueryableAsync();
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            query.Where(si => si.StandardId == standardId && si.Id == id)
        );

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("StandardIndicator", id);
        }

        entity.IsActive = !entity.IsActive;
        await _standardIndicatorRepository.UpdateAsync(entity);

        // Get indicator and test method info for response
        var indicator = await _indicatorRepository.FindAsync(entity.IndicatorId);
        var testMethod = await _testMethodRepository.FindAsync(entity.TestMethodId);

        return new StandardIndicatorDto
        {
            Id = entity.Id,
            StandardId = entity.StandardId,
            IndicatorId = entity.IndicatorId,
            IndicatorCode = indicator?.Code ?? "",
            IndicatorName = indicator?.Name ?? "",
            TestMethodId = entity.TestMethodId,
            TestMethodCode = testMethod?.Code ?? "",
            TestMethodName = testMethod?.Name ?? "",
            Unit = entity.Unit,
            ComparisonOperator = entity.ComparisonOperator,
            AllowedLimit = entity.AllowedLimit,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder
        };
    }

    private async Task<string> GenerateCodeAsync()
    {
        var today = DateTime.Now;
        var datePrefix = $"TC{today:ddMMyyyy}";

        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _standardRepository.GetQueryableAsync();
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
                    "Đã đạt giới hạn số tiêu chuẩn trong ngày (999). Vui lòng thử lại vào ngày mai.");
            }

            return $"{datePrefix}{nextNumber:D3}";
        }
    }
}
