using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Dịch vụ (cấp 1 của L2 Dịch vụ & Bảng giá, vd "Kiểm nghiệm thực phẩm"): gom nhiều dòng TestMethodIndicator
/// (phương pháp × chỉ tiêu × nền mẫu) thuộc nhiều nhóm chỉ tiêu khác nhau. Khác Nhóm chỉ tiêu (chỉ dùng để lọc).
/// </summary>
public class ServiceGroup : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>Mã dịch vụ (NDV0001...) — DB sinh bằng sequence</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    protected ServiceGroup()
    {
    }

    /// <param name="code">null → DB sinh mã (sequence) và EF đọc lại sau insert</param>
    public ServiceGroup(Guid id, string? code, string name) : base(id)
    {
        Code = code!;
        Name = name;
        IsActive = true;
    }
}
