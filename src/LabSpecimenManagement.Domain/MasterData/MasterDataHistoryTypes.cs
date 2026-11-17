using System;
using System.Collections.Generic;
using System.Linq;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Danh sách entity master data được ABP ghi lịch sử thay đổi (AbpEntityChanges / AbpEntityPropertyChanges).
/// Key trùng với category key bên FE để API lịch sử nhận diện loại danh mục.
/// </summary>
public static class MasterDataHistoryTypes
{
    public static readonly IReadOnlyDictionary<string, Type> ByKey = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        ["departments"] = typeof(Department),
        ["laboratories"] = typeof(Laboratory),
        ["sampleTypeCategories"] = typeof(SampleTypeCategory),
        ["sampleConditions"] = typeof(SampleCondition),
        ["specimenTypes"] = typeof(SpecimenType),
        ["testMethods"] = typeof(TestMethod),
        ["standards"] = typeof(Standard),
        ["storageLocations"] = typeof(StorageLocation),
        ["indicators"] = typeof(Indicator),
        ["indicatorGroups"] = typeof(IndicatorGroup),
        ["testResultValues"] = typeof(TestResultValue),
        ["notes"] = typeof(Note),
        ["customers"] = typeof(Customer),
        ["organizations"] = typeof(Organization),
        ["services"] = typeof(TestMethodIndicator),
        ["batchNorms"] = typeof(MethodBatchNorm),
        ["yearlyForecasts"] = typeof(ServiceYearlyForecast),
    };

    /// <summary>
    /// Bảng liên kết (không có màn hình riêng) — vẫn ghi lịch sử để tra cứu khi cần.
    /// </summary>
    private static readonly Type[] LinkTypes =
    {
        typeof(StandardIndicator),
        typeof(SpecimenTypeIndicator),
        typeof(TestResultValueIndicator),
        typeof(ServicePriceVersion),
    };

    private static readonly HashSet<Type> TrackedTypes = ByKey.Values.Concat(LinkTypes).ToHashSet();

    public static bool IsTracked(Type type) => TrackedTypes.Contains(type);
}
