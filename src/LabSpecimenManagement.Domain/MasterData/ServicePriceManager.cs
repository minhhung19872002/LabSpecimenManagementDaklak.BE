using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Bảng giá dịch vụ theo phiên bản (ngày hiệu lực).
/// TestMethodIndicator.UnitPrice / TurnaroundDays luôn giữ giá đang áp dụng hôm nay —
/// các luồng lập phiếu / báo giá cũ đọc thẳng cột này nên không phải sửa.
/// </summary>
public class ServicePriceManager : DomainService
{
    private readonly IRepository<TestMethodIndicator, Guid> _serviceRepository;
    private readonly IRepository<ServicePriceVersion, Guid> _versionRepository;

    public ServicePriceManager(
        IRepository<TestMethodIndicator, Guid> serviceRepository,
        IRepository<ServicePriceVersion, Guid> versionRepository)
    {
        _serviceRepository = serviceRepository;
        _versionRepository = versionRepository;
    }

    public DateTime Today => Clock.Now.Date;

    /// <summary>
    /// Version áp dụng tại ngày: EffectiveDate lớn nhất ≤ ngày; chưa có → giá gốc (EffectiveDate null).
    /// </summary>
    public static ServicePriceVersion? ResolveAt(IEnumerable<ServicePriceVersion> versions, DateTime date)
    {
        var list = versions as IList<ServicePriceVersion> ?? versions.ToList();
        var day = date.Date;

        return list
                   .Where(v => v.EffectiveDate.HasValue && v.EffectiveDate.Value <= day)
                   .OrderByDescending(v => v.EffectiveDate)
                   .FirstOrDefault()
               ?? list
                   .Where(v => !v.EffectiveDate.HasValue)
                   .OrderByDescending(v => v.CreationTime)
                   .FirstOrDefault();
    }

    /// <summary>Version sắp áp dụng gần nhất sau ngày</summary>
    public static ServicePriceVersion? NextAfter(IEnumerable<ServicePriceVersion> versions, DateTime date)
    {
        var day = date.Date;
        return versions
            .Where(v => v.EffectiveDate.HasValue && v.EffectiveDate.Value > day)
            .OrderBy(v => v.EffectiveDate)
            .FirstOrDefault();
    }

    public async Task<List<ServicePriceVersion>> GetVersionsAsync(Guid serviceId)
    {
        return await _versionRepository.GetListAsync(v => v.TestMethodIndicatorId == serviceId);
    }

    public async Task<Dictionary<Guid, List<ServicePriceVersion>>> GetVersionsAsync(ICollection<Guid> serviceIds)
    {
        if (serviceIds.Count == 0)
        {
            return new Dictionary<Guid, List<ServicePriceVersion>>();
        }

        var versions = await _versionRepository.GetListAsync(v => serviceIds.Contains(v.TestMethodIndicatorId));
        return versions.GroupBy(v => v.TestMethodIndicatorId).ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Thêm / sửa version tại ngày hiệu lực (trùng ngày → sửa version đó) rồi đồng bộ giá hôm nay.
    /// </summary>
    public async Task<ServicePriceVersion> SetPriceAsync(
        TestMethodIndicator service,
        decimal unitPrice,
        int? turnaroundDays,
        DateTime effectiveDate,
        string? note,
        List<ServicePriceVersion>? versions = null)
    {
        versions ??= await GetVersionsAsync(service.Id);
        var day = effectiveDate.Date;

        var version = versions.FirstOrDefault(v => v.EffectiveDate == day);
        if (version == null)
        {
            version = new ServicePriceVersion(GuidGenerator.Create(), service.Id, unitPrice, turnaroundDays, day, note);
            await _versionRepository.InsertAsync(version, autoSave: true);
            versions.Add(version);
        }
        else
        {
            version.UnitPrice = unitPrice;
            version.TurnaroundDays = turnaroundDays;
            version.Note = note ?? version.Note;
            await _versionRepository.UpdateAsync(version, autoSave: true);
        }

        await SyncCurrentPriceAsync(service, versions);
        return version;
    }

    /// <summary>Ghi giá đang áp dụng hôm nay vào TestMethodIndicator (chỉ update khi khác)</summary>
    public async Task<bool> SyncCurrentPriceAsync(TestMethodIndicator service, IEnumerable<ServicePriceVersion> versions)
    {
        var current = ResolveAt(versions, Today);
        if (current == null)
        {
            return false;
        }

        if (service.UnitPrice == current.UnitPrice && service.TurnaroundDays == current.TurnaroundDays)
        {
            return false;
        }

        service.UnitPrice = current.UnitPrice;
        service.TurnaroundDays = current.TurnaroundDays;
        await _serviceRepository.UpdateAsync(service, autoSave: true);
        return true;
    }

    /// <summary>
    /// Áp các version đã đến ngày hiệu lực. Trả về số dịch vụ đổi giá.
    /// </summary>
    public async Task<int> ApplyDuePricesAsync()
    {
        var today = Today;

        // Chỉ dịch vụ có version đến hạn trong 1 năm gần đây mới có thể lệch giá
        var from = today.AddYears(-1);
        var dueServiceIds = (await _versionRepository.GetListAsync(v =>
                v.EffectiveDate != null && v.EffectiveDate <= today && v.EffectiveDate >= from))
            .Select(v => v.TestMethodIndicatorId)
            .Distinct()
            .ToList();

        if (dueServiceIds.Count == 0)
        {
            return 0;
        }

        var services = await _serviceRepository.GetListAsync(s => dueServiceIds.Contains(s.Id));
        var versionsByService = await GetVersionsAsync(dueServiceIds);

        var changed = 0;
        foreach (var service in services)
        {
            if (versionsByService.TryGetValue(service.Id, out var versions)
                && await SyncCurrentPriceAsync(service, versions))
            {
                changed++;
            }
        }

        return changed;
    }
}
