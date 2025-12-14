using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Loại mẫu - Specimen Type
/// </summary>
public class SpecimenType : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Mã loại mẫu - Specimen Type Code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên loại mẫu - Specimen Type Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Mô tả - Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Điều kiện bảo quản mặc định
    /// </summary>
    public string? DefaultStorageCondition { get; set; }

    /// <summary>
    /// Thời gian lưu trữ tối đa (ngày)
    /// </summary>
    public int? MaxStorageDays { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected SpecimenType()
    {
    }

    public SpecimenType(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
