using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData.Snapshots;

/// <summary>
/// Snapshot master data cho chứng từ (phiếu yêu cầu, task phòng TN, kết quả...).
///
/// Nguyên tắc:
/// - Khi tạo phiếu / đổi FK master → lưu snapshot (tên, mã...) vào chứng từ (<see cref="SnapshotAsync(IEnumerable{TestRequestSampleIndicator}, bool)"/>...).
/// - Khi hiển thị/in/xuất → ưu tiên snapshot, thiếu mới lấy master (<see cref="Pick"/>).
/// - Tra cứu master cho chứng từ luôn bao gồm bản ghi đã xóa mềm (<see cref="GetListIncludingDeletedAsync{TEntity}"/>)
///   để chứng từ cũ không bị lỗi / mất dòng / trống tên khi danh mục bị xóa.
/// </summary>
public class MasterDataSnapshotService : ITransientDependency
{
    private readonly IAbpLazyServiceProvider _lazyServiceProvider;
    private readonly IDataFilter _dataFilter;

    public MasterDataSnapshotService(IAbpLazyServiceProvider lazyServiceProvider, IDataFilter dataFilter)
    {
        _lazyServiceProvider = lazyServiceProvider;
        _dataFilter = dataFilter;
    }

    /// <summary>Ưu tiên giá trị snapshot, rỗng thì dùng giá trị master</summary>
    public static string? Pick(string? snapshot, string? master)
        => !string.IsNullOrWhiteSpace(snapshot) ? snapshot : master;

    #region Tra cứu master (bao gồm bản ghi đã xóa mềm)

    /// <summary>
    /// Lấy danh sách master theo điều kiện, BAO GỒM bản ghi đã xóa mềm.
    /// Chỉ dùng để hiển thị chứng từ — không dùng cho danh sách chọn (dropdown).
    /// </summary>
    public async Task<List<TEntity>> GetListIncludingDeletedAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class, IEntity<Guid>
    {
        var repository = _lazyServiceProvider.LazyGetRequiredService<IRepository<TEntity, Guid>>();
        using (_dataFilter.Disable<ISoftDelete>())
        {
            return await repository.GetListAsync(predicate);
        }
    }

    /// <summary>
    /// Dictionary Id → master theo danh sách Id, BAO GỒM bản ghi đã xóa mềm.
    /// </summary>
    public async Task<Dictionary<Guid, TEntity>> GetDictionaryIncludingDeletedAsync<TEntity>(IEnumerable<Guid?> ids)
        where TEntity : class, IEntity<Guid>
    {
        return await GetDictionaryIncludingDeletedAsync<TEntity>(ids.Where(x => x.HasValue).Select(x => x!.Value));
    }

    /// <summary>
    /// Dictionary Id → master theo danh sách Id, BAO GỒM bản ghi đã xóa mềm.
    /// </summary>
    public async Task<Dictionary<Guid, TEntity>> GetDictionaryIncludingDeletedAsync<TEntity>(IEnumerable<Guid> ids)
        where TEntity : class, IEntity<Guid>
    {
        var idList = ids.Where(x => x != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, TEntity>();
        }

        var items = await GetListIncludingDeletedAsync<TEntity>(x => idList.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    /// <summary>
    /// Tìm 1 master theo Id, BAO GỒM bản ghi đã xóa mềm. Trả null nếu không có.
    /// </summary>
    public async Task<TEntity?> FindIncludingDeletedAsync<TEntity>(Guid? id)
        where TEntity : class, IEntity<Guid>
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return null;
        }

        var repository = _lazyServiceProvider.LazyGetRequiredService<IRepository<TEntity, Guid>>();
        using (_dataFilter.Disable<ISoftDelete>())
        {
            return await repository.FindAsync(id.Value);
        }
    }

    /// <summary>
    /// Tên khoa phòng (= tên Department của Laboratory) theo LaboratoryId, BAO GỒM bản ghi đã xóa mềm.
    /// </summary>
    public async Task<Dictionary<Guid, string>> GetLaboratoryNamesIncludingDeletedAsync(IEnumerable<Guid?> laboratoryIds)
    {
        var labs = await GetDictionaryIncludingDeletedAsync<Laboratory>(laboratoryIds);
        var departments = await GetDictionaryIncludingDeletedAsync<Department>(labs.Values.Select(x => (Guid?)x.DepartmentId));

        return labs.Values
            .Where(lab => departments.ContainsKey(lab.DepartmentId))
            .ToDictionary(lab => lab.Id, lab => departments[lab.DepartmentId].Name);
    }

    /// <summary>
    /// Tên khoa phòng theo LaboratoryId, BAO GỒM bản ghi đã xóa mềm.
    /// </summary>
    public async Task<Dictionary<Guid, string>> GetLaboratoryNamesIncludingDeletedAsync(IEnumerable<Guid> laboratoryIds)
    {
        return await GetLaboratoryNamesIncludingDeletedAsync(laboratoryIds.Select(x => (Guid?)x));
    }

    #endregion

    #region Ghi snapshot vào chứng từ

    /// <summary>
    /// Ghi snapshot cho chỉ tiêu của mẫu (chỉ tiêu, nhóm, phương pháp, khoa phòng, loại mẫu).
    /// overwrite = false: chỉ điền phần đang trống (dùng khi tạo mới).
    /// overwrite = true: ghi đè theo FK hiện tại (dùng cho các dòng vừa đổi FK).
    /// </summary>
    public async Task SnapshotAsync(IEnumerable<TestRequestSampleIndicator> items, bool overwrite = false)
    {
        var list = items.ToList();
        if (list.Count == 0)
        {
            return;
        }

        // Chỉ tra master cho phần cần điền — dòng cũ đã đủ snapshot (sửa phiếu không đổi gì) không tốn query
        var indicators = await GetDictionaryIncludingDeletedAsync<Indicator>(list
            .Where(x => overwrite || string.IsNullOrWhiteSpace(x.IndicatorName))
            .Select(x => x.IndicatorId));
        var groups = await GetDictionaryIncludingDeletedAsync<IndicatorGroup>(indicators.Values.Select(x => x.IndicatorGroupId));
        var methods = await GetDictionaryIncludingDeletedAsync<TestMethod>(list
            .Where(x => overwrite || string.IsNullOrWhiteSpace(x.TestMethodName))
            .Select(x => x.TestMethodId));
        var labNames = await GetLaboratoryNamesIncludingDeletedAsync(list
            .Where(x => overwrite || string.IsNullOrWhiteSpace(x.LaboratoryName))
            .Select(x => x.LaboratoryId));
        var specimenTypes = await GetDictionaryIncludingDeletedAsync<SpecimenType>(list
            .Where(x => overwrite || string.IsNullOrWhiteSpace(x.SpecimenTypeName))
            .Select(x => x.SpecimenTypeId));

        foreach (var item in list)
        {
            if ((overwrite || string.IsNullOrWhiteSpace(item.IndicatorName))
                && indicators.TryGetValue(item.IndicatorId, out var indicator))
            {
                item.IndicatorCode = indicator.Code;
                item.IndicatorName = indicator.Name;
                item.IndicatorFormattedName = indicator.FormattedName;
                item.IndicatorGroupName = indicator.IndicatorGroupId is Guid groupId && groups.TryGetValue(groupId, out var group) ? group.Name : null;
            }

            if (overwrite || string.IsNullOrWhiteSpace(item.TestMethodName))
            {
                if (item.TestMethodId.HasValue && methods.TryGetValue(item.TestMethodId.Value, out var method))
                {
                    item.TestMethodCode = method.Code;
                    item.TestMethodName = method.Name;
                    item.TestMethodFormattedName = method.FormattedName;
                    item.TestMethodUnit = method.Unit;
                    item.TestMethodFormattedUnit = method.FormattedUnit;
                }
                else if (!item.TestMethodId.HasValue)
                {
                    item.TestMethodCode = null;
                    item.TestMethodName = null;
                    item.TestMethodFormattedName = null;
                    item.TestMethodUnit = null;
                    item.TestMethodFormattedUnit = null;
                }
            }

            if (overwrite || string.IsNullOrWhiteSpace(item.LaboratoryName))
            {
                item.LaboratoryName = item.LaboratoryId.HasValue && labNames.TryGetValue(item.LaboratoryId.Value, out var labName)
                    ? labName
                    : null;
            }

            if (overwrite || string.IsNullOrWhiteSpace(item.SpecimenTypeName))
            {
                item.SpecimenTypeName = item.SpecimenTypeId.HasValue && specimenTypes.TryGetValue(item.SpecimenTypeId.Value, out var specimenType)
                    ? specimenType.Name
                    : null;
            }
        }
    }

    public Task SnapshotAsync(TestRequestSampleIndicator item, bool overwrite = false)
        => SnapshotAsync(new[] { item }, overwrite);

    /// <summary>
    /// Ghi snapshot tên loại mẫu cho mẫu.
    /// </summary>
    public async Task SnapshotAsync(IEnumerable<TestRequestSample> samples, bool overwrite = false)
    {
        var list = samples.ToList();
        if (list.Count == 0)
        {
            return;
        }

        var specimenTypes = await GetDictionaryIncludingDeletedAsync<SpecimenType>(list
            .Where(x => overwrite || string.IsNullOrWhiteSpace(x.SpecimenTypeName))
            .Select(x => x.SpecimenTypeId));
        foreach (var sample in list)
        {
            if (overwrite || string.IsNullOrWhiteSpace(sample.SpecimenTypeName))
            {
                sample.SpecimenTypeName = sample.SpecimenTypeId.HasValue && specimenTypes.TryGetValue(sample.SpecimenTypeId.Value, out var specimenType)
                    ? specimenType.Name
                    : null;
            }
        }
    }

    public Task SnapshotAsync(TestRequestSample sample, bool overwrite = false)
        => SnapshotAsync(new[] { sample }, overwrite);

    /// <summary>
    /// Ghi snapshot tên khoa phòng cho task phòng thí nghiệm.
    /// </summary>
    public async Task SnapshotAsync(IEnumerable<LaboratoryTask> tasks, bool overwrite = false)
    {
        var list = tasks.ToList();
        if (list.Count == 0)
        {
            return;
        }

        var labNames = await GetLaboratoryNamesIncludingDeletedAsync(list
            .Where(x => overwrite || string.IsNullOrWhiteSpace(x.LaboratoryName))
            .Select(x => x.LaboratoryId));
        foreach (var task in list)
        {
            if (overwrite || string.IsNullOrWhiteSpace(task.LaboratoryName))
            {
                task.LaboratoryName = labNames.TryGetValue(task.LaboratoryId, out var labName) ? labName : null;
            }
        }
    }

    public Task SnapshotAsync(LaboratoryTask task, bool overwrite = false)
        => SnapshotAsync(new[] { task }, overwrite);

    /// <summary>
    /// Ghi snapshot phương pháp được gán (MethodId) và tiêu chuẩn (StandardId) cho chỉ tiêu của task.
    /// </summary>
    public async Task SnapshotAsync(IEnumerable<LaboratoryTaskIndicator> items, bool overwrite = false)
    {
        var list = items.ToList();
        if (list.Count == 0)
        {
            return;
        }

        var methods = await GetDictionaryIncludingDeletedAsync<TestMethod>(list
            .Where(x => overwrite || string.IsNullOrWhiteSpace(x.MethodName))
            .Select(x => x.MethodId));
        var standards = await GetDictionaryIncludingDeletedAsync<Standard>(list
            .Where(x => overwrite || string.IsNullOrWhiteSpace(x.StandardName))
            .Select(x => x.StandardId));

        foreach (var item in list)
        {
            if (overwrite || string.IsNullOrWhiteSpace(item.MethodName))
            {
                if (item.MethodId.HasValue && methods.TryGetValue(item.MethodId.Value, out var method))
                {
                    item.MethodCode = method.Code;
                    item.MethodName = method.Name;
                    item.MethodFormattedName = method.FormattedName;
                }
                else if (!item.MethodId.HasValue)
                {
                    item.MethodCode = null;
                    item.MethodName = null;
                    item.MethodFormattedName = null;
                }
            }

            if (overwrite || string.IsNullOrWhiteSpace(item.StandardName))
            {
                item.StandardName = item.StandardId.HasValue && standards.TryGetValue(item.StandardId.Value, out var standard)
                    ? standard.Name
                    : null;
            }
        }
    }

    public Task SnapshotAsync(LaboratoryTaskIndicator item, bool overwrite = false)
        => SnapshotAsync(new[] { item }, overwrite);

    #endregion
}
