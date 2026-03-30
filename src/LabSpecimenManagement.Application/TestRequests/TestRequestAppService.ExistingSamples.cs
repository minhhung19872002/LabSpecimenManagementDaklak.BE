using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Partial class for getting all samples across test requests (for "Thêm mẫu có sẵn" feature)
/// </summary>
public partial class TestRequestAppService
{
    public async Task<PagedResultDto<ExistingSampleDto>> GetAllSamplesAsync(GetAllSamplesInput input)
    {
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var testRequestQuery = await Repository.GetQueryableAsync();

        var joinedQuery = from sample in sampleQuery
                          join tr in testRequestQuery on sample.TestRequestId equals tr.Id
                          where tr.Status != TestRequestStatus.Cancelled
                            && !sample.IsCancelled
                            && !sample.IsCustomerCancelled
                          select new { Sample = sample, tr.ContractNumber };

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            joinedQuery = joinedQuery.Where(x =>
                x.Sample.SampleCode.ToLower().Contains(search) ||
                x.Sample.SampleName.ToLower().Contains(search));
        }

        // Sort by contract number descending
        joinedQuery = joinedQuery.OrderByDescending(x => x.ContractNumber)
                                 .ThenByDescending(x => x.Sample.SortOrder);

        var totalCount = await AsyncExecuter.CountAsync(joinedQuery);

        var pagedQuery = joinedQuery
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var items = await AsyncExecuter.ToListAsync(pagedQuery);

        // Load indicators for each sample
        var sampleIds = items.Select(x => x.Sample.Id).ToList();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var allIndicators = await AsyncExecuter.ToListAsync(
            sampleIndicatorQuery.Where(si => sampleIds.Contains(si.TestRequestSampleId))
        );
        var indicatorsBySample = allIndicators.GroupBy(si => si.TestRequestSampleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Load master data names
        var indicatorIds = allIndicators.Select(i => i.IndicatorId).Distinct().ToList();
        var testMethodIds = allIndicators.Where(i => i.TestMethodId.HasValue)
            .Select(i => i.TestMethodId!.Value).Distinct().ToList();
        var specimenTypeIds = allIndicators.Where(i => i.SpecimenTypeId.HasValue)
            .Select(i => i.SpecimenTypeId!.Value).Distinct().ToList();

        // Add sample-level specimen type IDs
        var sampleSpecimenTypeIds = items.Where(x => x.Sample.SpecimenTypeId.HasValue)
            .Select(x => x.Sample.SpecimenTypeId!.Value).Distinct().ToList();
        var allSpecimenTypeIds = specimenTypeIds.Union(sampleSpecimenTypeIds).Distinct().ToList();

        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var indicatorList = await AsyncExecuter.ToListAsync(
            indicatorQuery.Where(i => indicatorIds.Contains(i.Id))
        );

        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();
        var testMethodList = await AsyncExecuter.ToListAsync(
            testMethodQuery.Where(m => testMethodIds.Contains(m.Id))
        );

        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();
        var specimenTypeList = await AsyncExecuter.ToListAsync(
            specimenTypeQuery.Where(st => allSpecimenTypeIds.Contains(st.Id))
        );

        var indicatorLookup = indicatorList.ToDictionary(i => i.Id);
        var testMethodLookup = testMethodList.ToDictionary(m => m.Id);
        var specimenTypeLookup = specimenTypeList.ToDictionary(st => st.Id);

        var result = items.Select(x =>
        {
            var sampleIndicators = indicatorsBySample.GetValueOrDefault(x.Sample.Id, new List<TestRequestSampleIndicator>());

            return new ExistingSampleDto
            {
                SampleId = x.Sample.Id,
                SampleCode = x.Sample.SampleCode,
                SampleName = x.Sample.SampleName,
                SampleQuantity = x.Sample.SampleQuantity,
                SpecimenTypeId = x.Sample.SpecimenTypeId,
                SpecimenTypeName = x.Sample.SpecimenTypeId.HasValue && specimenTypeLookup.ContainsKey(x.Sample.SpecimenTypeId.Value)
                    ? specimenTypeLookup[x.Sample.SpecimenTypeId.Value].Name : null,
                Status = x.Sample.Status,
                StatusText = x.Sample.StatusText,
                ContractNumber = x.ContractNumber,
                Indicators = sampleIndicators.Select(ind =>
                {
                    indicatorLookup.TryGetValue(ind.IndicatorId, out var indicatorInfo);
                    var testMethodInfo = ind.TestMethodId.HasValue && testMethodLookup.ContainsKey(ind.TestMethodId.Value)
                        ? testMethodLookup[ind.TestMethodId.Value] : null;
                    var specimenTypeInfo = ind.SpecimenTypeId.HasValue && specimenTypeLookup.ContainsKey(ind.SpecimenTypeId.Value)
                        ? specimenTypeLookup[ind.SpecimenTypeId.Value] : null;

                    return new ExistingSampleIndicatorDto
                    {
                        IndicatorId = ind.IndicatorId,
                        IndicatorName = indicatorInfo?.Name,
                        IndicatorFormattedName = indicatorInfo?.FormattedName,
                        TestMethodId = ind.TestMethodId,
                        TestMethodName = testMethodInfo?.Name,
                        TestMethodFormattedName = testMethodInfo?.FormattedName,
                        SpecimenTypeId = ind.SpecimenTypeId,
                        SpecimenTypeName = specimenTypeInfo?.Name,
                        UnitPrice = ind.UnitPrice,
                    };
                }).ToList(),
            };
        }).ToList();

        return new PagedResultDto<ExistingSampleDto>(totalCount, result);
    }
}
