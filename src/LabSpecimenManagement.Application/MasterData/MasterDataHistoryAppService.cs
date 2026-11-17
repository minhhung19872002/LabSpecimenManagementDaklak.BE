using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.Histories;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Đọc lịch sử thay đổi master data từ entity history của ABP
/// (cấu hình selector tại LabSpecimenManagementDomainModule / MasterDataHistoryTypes).
/// </summary>
[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class MasterDataHistoryAppService : ApplicationService, IMasterDataHistoryAppService
{
    private const string ActionCreated = "Tạo mới";
    private const string ActionUpdated = "Sửa";
    private const string ActionDeleted = "Xóa";
    private const string ActionDeactivated = "Ngừng hoạt động";
    private const string ActionReactivated = "Kích hoạt lại";

    /// <summary>
    /// Trường kỹ thuật không hiển thị cho người dùng
    /// </summary>
    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "TenantId", "ConcurrencyStamp", "ExtraProperties",
        "CreationTime", "CreatorId", "LastModificationTime", "LastModifierId",
        "IsDeleted", "DeleterId", "DeletionTime",
    };

    private static readonly Dictionary<string, string> PropertyLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Code"] = "Mã",
        ["Name"] = "Tên",
        ["FormattedName"] = "Tên định dạng",
        ["Description"] = "Mô tả",
        ["Content"] = "Nội dung",
        ["IsActive"] = "Trạng thái",
        ["SortOrder"] = "Thứ tự",
        ["IndicatorGroupId"] = "Nhóm chỉ tiêu",
        ["ParentIndicatorId"] = "Chỉ tiêu cha",
        ["NeedsMethodSetup"] = "Cần cập nhật phương pháp",
        ["LaboratoryId"] = "Khoa phòng",
        ["DepartmentId"] = "Phòng ban",
        ["TechnicalManagerId"] = "Quản lý kỹ thuật",
        ["SampleTypeCategoryId"] = "Danh mục loại mẫu",
        ["DefaultStorageCondition"] = "Điều kiện bảo quản",
        ["MaxStorageDays"] = "Thời gian lưu tối đa (ngày)",
        ["Unit"] = "Đơn vị tính",
        ["FormattedUnit"] = "Đơn vị tính định dạng",
        ["LimitDescription"] = "Giới hạn",
        ["Temperature"] = "Nhiệt độ",
        ["Humidity"] = "Độ ẩm",
        ["Capacity"] = "Sức chứa",
        ["CurrentCount"] = "Số lượng hiện tại",
        ["Email"] = "Email",
        ["Phone"] = "Số điện thoại",
        ["SecondaryPhone"] = "Số điện thoại phụ",
        ["Address"] = "Địa chỉ",
        ["TaxCode"] = "Mã số thuế",
        ["ContactPerson"] = "Người liên hệ",
        ["ContactPhone"] = "SĐT người liên hệ",
        ["Notes"] = "Ghi chú",
        ["ProvinceCode"] = "Tỉnh/thành",
        ["WardCode"] = "Phường/xã",
        // L2 Dịch vụ & Bảng giá
        ["ServiceCode"] = "Mã dịch vụ",
        ["TestMethodId"] = "Phương pháp",
        ["IndicatorId"] = "Chỉ tiêu",
        ["SpecimenTypeId"] = "Nền mẫu",
        ["UnitPrice"] = "Đơn giá",
        ["TurnaroundDays"] = "Thời gian trả KQ (ngày)",
        ["IsSubcontract"] = "HĐ phụ",
        ["EffectiveDate"] = "Ngày hiệu lực",
        ["Note"] = "Ghi chú",
        // L3 Định mức theo mẻ
        ["ProductId"] = "Vật tư",
        ["UnitId"] = "Đơn vị",
        ["BatchQuantity"] = "Tổng tiêu hao / mẻ",
        // L4 Dự toán mẫu năm
        ["Year"] = "Năm",
        ["EstimatedSamples"] = "Số mẫu dự toán",
        ["TestMethodIndicatorId"] = "Dịch vụ",
    };

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<Products.Product, Guid> _productRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<SampleTypeCategory, Guid> _sampleTypeCategoryRepository;
    private readonly IUserNameProvider _userNameProvider;

    public MasterDataHistoryAppService(
        IAuditLogRepository auditLogRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<SampleTypeCategory, Guid> sampleTypeCategoryRepository,
        IUserNameProvider userNameProvider,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<Products.Product, Guid> productRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository)
    {
        _auditLogRepository = auditLogRepository;
        _testMethodRepository = testMethodRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _unitRepository = unitRepository;
        _productRepository = productRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _indicatorRepository = indicatorRepository;
        _departmentRepository = departmentRepository;
        _laboratoryRepository = laboratoryRepository;
        _sampleTypeCategoryRepository = sampleTypeCategoryRepository;
        _userNameProvider = userNameProvider;
    }

    public async Task<ListResultDto<MasterDataChangeDto>> GetListAsync(string entityType, Guid entityId)
    {
        if (!MasterDataHistoryTypes.ByKey.TryGetValue(entityType ?? string.Empty, out var type))
        {
            throw new UserFriendlyException($"Loại danh mục '{entityType}' không hỗ trợ xem lịch sử.");
        }

        var changes = await _auditLogRepository.GetEntityChangesWithUsernameAsync(
            entityId.ToString(), type.FullName!);

        var guidLookup = await BuildGuidLookupAsync(changes.Select(c => c.EntityChange));

        var result = changes
            .OrderByDescending(c => c.EntityChange.ChangeTime)
            .Select(c => MapChange(c, guidLookup))
            .Where(dto => dto != null)
            .Select(dto => dto!)
            .ToList();

        return new ListResultDto<MasterDataChangeDto>(result);
    }

    private static MasterDataChangeDto? MapChange(EntityChangeWithUsername item, Dictionary<Guid, string> guidLookup)
    {
        var change = item.EntityChange;
        var properties = change.PropertyChanges
            .Where(p => !IgnoredProperties.Contains(p.PropertyName))
            .Select(p => new MasterDataPropertyChangeDto
            {
                PropertyName = p.PropertyName,
                DisplayName = PropertyLabels.GetValueOrDefault(p.PropertyName, p.PropertyName),
                OriginalValue = FormatValue(p.PropertyName, p.OriginalValue, guidLookup),
                NewValue = FormatValue(p.PropertyName, p.NewValue, guidLookup),
            })
            // Tạo mới: bỏ trường rỗng; Sửa: bỏ trường không đổi giá trị hiển thị
            .Where(p => change.ChangeType == EntityChangeType.Created
                ? !string.IsNullOrEmpty(p.NewValue)
                : p.OriginalValue != p.NewValue)
            .ToList();

        // Update chỉ chạm trường kỹ thuật (LastModificationTime...) → không hiển thị
        if (change.ChangeType == EntityChangeType.Updated && properties.Count == 0)
        {
            return null;
        }

        return new MasterDataChangeDto
        {
            Id = change.Id,
            ChangeTime = change.ChangeTime,
            ChangeType = change.ChangeType.ToString(),
            Action = ResolveAction(change),
            UserName = item.UserName,
            Properties = change.ChangeType == EntityChangeType.Deleted ? new() : properties,
        };
    }

    private static string ResolveAction(EntityChange change)
    {
        switch (change.ChangeType)
        {
            case EntityChangeType.Created:
                return ActionCreated;
            case EntityChangeType.Deleted:
                return ActionDeleted;
        }

        var isActiveChange = change.PropertyChanges.FirstOrDefault(p =>
            string.Equals(p.PropertyName, "IsActive", StringComparison.OrdinalIgnoreCase)
            && p.OriginalValue != p.NewValue);
        if (isActiveChange == null)
        {
            return ActionUpdated;
        }

        var statusAction = isActiveChange.NewValue == "false" ? ActionDeactivated : ActionReactivated;
        var hasOtherChanges = change.PropertyChanges.Any(p =>
            !IgnoredProperties.Contains(p.PropertyName)
            && !string.Equals(p.PropertyName, "IsActive", StringComparison.OrdinalIgnoreCase)
            && p.OriginalValue != p.NewValue);

        return hasOtherChanges ? $"{ActionUpdated}, {statusAction.ToLowerInvariant()}" : statusAction;
    }

    /// <summary>
    /// ABP lưu giá trị dạng JSON ("\"abc\"", true, 12...) → đổi sang chuỗi đọc được
    /// </summary>
    private static string? FormatValue(string propertyName, string? raw, Dictionary<Guid, string> guidLookup)
    {
        if (raw == null || raw == "null")
        {
            return null;
        }

        var value = raw;
        if (raw.Length >= 2 && raw[0] == '"')
        {
            try
            {
                value = JsonSerializer.Deserialize<string>(raw) ?? string.Empty;
            }
            catch (JsonException)
            {
                // Giá trị bị ABP cắt ngắn (quá 512 ký tự) → không parse được JSON, bỏ dấu nháy đầu
                value = raw.TrimStart('"');
            }
        }

        if (raw == "true" || raw == "false")
        {
            var isTrue = raw == "true";
            return string.Equals(propertyName, "IsActive", StringComparison.OrdinalIgnoreCase)
                ? (isTrue ? "Hoạt động" : "Ngừng hoạt động")
                : (isTrue ? "Có" : "Không");
        }

        if (propertyName.EndsWith("Id", StringComparison.Ordinal)
            && Guid.TryParse(value, out var guid)
            && guidLookup.TryGetValue(guid, out var name))
        {
            return name;
        }

        return value;
    }

    /// <summary>
    /// Đổi các khóa ngoại (nhóm chỉ tiêu, phòng ban, ...) sang tên — kể cả bản ghi đã xóa mềm
    /// </summary>
    private async Task<Dictionary<Guid, string>> BuildGuidLookupAsync(IEnumerable<EntityChange> changes)
    {
        var idsByProperty = changes
            .SelectMany(c => c.PropertyChanges)
            .Where(p => p.PropertyName.EndsWith("Id", StringComparison.Ordinal))
            .SelectMany(p => new[] { p.OriginalValue, p.NewValue }.Select(v => (p.PropertyName, Value: v)))
            .Select(x => (x.PropertyName, Id: ParseGuid(x.Value)))
            .Where(x => x.Id.HasValue)
            .GroupBy(x => x.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id!.Value).Distinct().ToList());

        var lookup = new Dictionary<Guid, string>();
        if (idsByProperty.Count == 0)
        {
            return lookup;
        }

        List<Guid> Ids(string property) => idsByProperty.GetValueOrDefault(property) ?? new List<Guid>();

        using (DataFilter.Disable<ISoftDelete>())
        {
            var groupIds = Ids("IndicatorGroupId");
            if (groupIds.Count > 0)
            {
                foreach (var g in await _indicatorGroupRepository.GetListAsync(x => groupIds.Contains(x.Id)))
                    lookup[g.Id] = g.Name;
            }

            var methodIds = Ids("TestMethodId");
            if (methodIds.Count > 0)
            {
                foreach (var m in await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id)))
                    lookup[m.Id] = $"{m.Code} - {m.Name}";
            }

            var specimenTypeIds = Ids("SpecimenTypeId");
            if (specimenTypeIds.Count > 0)
            {
                foreach (var s in await _specimenTypeRepository.GetListAsync(x => specimenTypeIds.Contains(x.Id)))
                    lookup[s.Id] = s.Name;
            }

            var unitIds = Ids("UnitId");
            if (unitIds.Count > 0)
            {
                foreach (var u in await _unitRepository.GetListAsync(x => unitIds.Contains(x.Id)))
                    lookup[u.Id] = u.Name;
            }

            var productIds = Ids("ProductId");
            if (productIds.Count > 0)
            {
                foreach (var p in await _productRepository.GetListAsync(x => productIds.Contains(x.Id)))
                    lookup[p.Id] = $"{p.Code} - {p.Name}";
            }

            var serviceIds = Ids("TestMethodIndicatorId");
            if (serviceIds.Count > 0)
            {
                foreach (var s in await _testMethodIndicatorRepository.GetListAsync(x => serviceIds.Contains(x.Id)))
                    lookup[s.Id] = s.ServiceCode ?? s.Id.ToString();
            }

            var parentIds = Ids("ParentIndicatorId").Concat(Ids("IndicatorId")).Distinct().ToList();
            if (parentIds.Count > 0)
            {
                foreach (var i in await _indicatorRepository.GetListAsync(x => parentIds.Contains(x.Id)))
                    lookup[i.Id] = $"{i.Code} - {i.Name}";
            }

            var categoryIds = Ids("SampleTypeCategoryId");
            if (categoryIds.Count > 0)
            {
                foreach (var c in await _sampleTypeCategoryRepository.GetListAsync(x => categoryIds.Contains(x.Id)))
                    lookup[c.Id] = c.Name;
            }

            var labIds = Ids("LaboratoryId");
            var labs = labIds.Count > 0
                ? await _laboratoryRepository.GetListAsync(x => labIds.Contains(x.Id))
                : new List<Laboratory>();

            var departmentIds = Ids("DepartmentId").Concat(labs.Select(l => l.DepartmentId)).Distinct().ToList();
            var departments = departmentIds.Count > 0
                ? (await _departmentRepository.GetListAsync(x => departmentIds.Contains(x.Id))).ToDictionary(d => d.Id, d => d.Name)
                : new Dictionary<Guid, string>();

            foreach (var d in departments)
                lookup[d.Key] = d.Value;
            foreach (var l in labs)
                lookup[l.Id] = departments.GetValueOrDefault(l.DepartmentId) ?? l.Code;
        }

        var userIds = Ids("TechnicalManagerId");
        if (userIds.Count > 0)
        {
            var users = await _userNameProvider.GetUserNamesAsync(userIds.Select(x => (Guid?)x));
            foreach (var u in users)
                lookup[u.Key] = u.Value;
        }

        return lookup;
    }

    private static Guid? ParseGuid(string? raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "null")
        {
            return null;
        }

        return Guid.TryParse(raw.Trim('"'), out var id) ? id : null;
    }
}
