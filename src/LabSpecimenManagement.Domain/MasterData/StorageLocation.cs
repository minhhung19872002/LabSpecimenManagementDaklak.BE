using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Vị trí lưu trữ - Storage Location
/// </summary>
public class StorageLocation : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã vị trí - Location Code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên vị trí - Location Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Mô tả - Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Nhiệt độ bảo quản (VD: 2-8°C, -20°C)
    /// </summary>
    public string? Temperature { get; set; }

    /// <summary>
    /// Độ ẩm (VD: 45-65%)
    /// </summary>
    public string? Humidity { get; set; }

    /// <summary>
    /// Sức chứa tối đa
    /// </summary>
    public int? Capacity { get; set; }

    /// <summary>
    /// Số lượng hiện tại
    /// </summary>
    public int CurrentCount { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected StorageLocation()
    {
    }

    public StorageLocation(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        CurrentCount = 0;
        SortOrder = 0;
    }
}
