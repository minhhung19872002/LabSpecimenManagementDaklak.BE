using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Phương pháp thử - Test Method
/// </summary>
public class TestMethod : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Mã phương pháp - Method Code (VD: TCVN6492, AOAC950)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên phương pháp đầy đủ
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Mô tả - Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tiêu chuẩn áp dụng (TCVN, ISO, ASTM...)
    /// </summary>
    public string? Standard { get; set; }

    /// <summary>
    /// Phòng ban thực hiện
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Thời gian thực hiện ước tính (giờ)
    /// </summary>
    public int? EstimatedDurationHours { get; set; }

    /// <summary>
    /// Giá dịch vụ (VND)
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected TestMethod()
    {
    }

    public TestMethod(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
