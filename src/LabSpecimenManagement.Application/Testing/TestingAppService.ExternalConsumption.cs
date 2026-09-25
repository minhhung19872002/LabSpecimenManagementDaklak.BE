using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Testing;

// TODO: EXTERNAL-CONSUMPTION — remove this entire file after BA confirms
// PROD-side sender: khi KTV submit kết quả, gửi tên PP/chỉ tiêu/loại mẫu sang UAT
// để UAT chạy luồng tiêu hao trên DB của UAT. Chỉ dùng repo/entity có sẵn trên main.
public partial class TestingAppService
{
    private IRepository<SpecimenType, Guid> _specimenTypeRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<SpecimenType, Guid>>();

    private ExternalConsumptionService _externalConsumptionService =>
        LazyServiceProvider.LazyGetRequiredService<ExternalConsumptionService>();

    internal async Task NotifyExternalConsumptionAsync(
        List<LaboratoryTaskIndicator> submittedIndicators, Guid laboratoryTaskId)
    {
        var indicatorsWithMethod = submittedIndicators.Where(i => i.MethodId.HasValue).ToList();
        if (!indicatorsWithMethod.Any()) return;

        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        var lab = await _laboratoryRepository.GetAsync(task.LaboratoryId);

        var sampleIndicatorIds = indicatorsWithMethod
            .Select(i => i.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            si => sampleIndicatorIds.Contains(si.Id));
        var sampleIndicatorMap = sampleIndicators.ToDictionary(si => si.Id);

        var indicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var specimenTypeIds = sampleIndicators.Where(si => si.SpecimenTypeId.HasValue).Select(si => si.SpecimenTypeId!.Value).Distinct().ToList();
        var methodIds = indicatorsWithMethod.Select(i => i.MethodId!.Value).Distinct().ToList();

        var indicatorEntities = await _indicatorRepository.GetListAsync(i => indicatorIds.Contains(i.Id));
        var indicatorNameMap = indicatorEntities.ToDictionary(i => i.Id, i => i.Name);

        var methodEntities = await _testMethodRepository.GetListAsync(m => methodIds.Contains(m.Id));
        var methodNameMap = methodEntities.ToDictionary(m => m.Id, m => m.Name);

        var stQ = await _specimenTypeRepository.GetQueryableAsync();
        var stEntities = await AsyncExecuter.ToListAsync(
            stQ.Where(s => specimenTypeIds.Contains(s.Id)));
        var stNameMap = stEntities.ToDictionary(s => s.Id, s => s.Name);

        var dtoIndicators = new List<ExternalConsumptionIndicatorDto>();
        foreach (var ind in indicatorsWithMethod)
        {
            if (!sampleIndicatorMap.TryGetValue(ind.TestRequestSampleIndicatorId, out var si)) continue;
            if (!methodNameMap.TryGetValue(ind.MethodId!.Value, out var methodName)) continue;
            if (!indicatorNameMap.TryGetValue(si.IndicatorId, out var indicatorName)) continue;
            if (!si.SpecimenTypeId.HasValue || !stNameMap.TryGetValue(si.SpecimenTypeId.Value, out var stName)) continue;

            dtoIndicators.Add(new ExternalConsumptionIndicatorDto
            {
                MethodName = methodName,
                IndicatorName = indicatorName,
                SpecimenTypeName = stName,
                TestDate = ind.TestDate.GetValueOrDefault(DateTime.Now),
            });
        }

        if (!dtoIndicators.Any()) return;

        await _externalConsumptionService.NotifyAsync(new ExternalConsumptionRequestDto
        {
            LaboratoryCode = lab.Code,
            Indicators = dtoIndicators
        });
    }
}
