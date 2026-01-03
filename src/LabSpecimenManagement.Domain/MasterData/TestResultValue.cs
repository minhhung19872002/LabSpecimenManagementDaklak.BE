using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Giá trị kết quả thử nghiệm - Test Result Value
/// Ví dụ: Đạt, Không đạt, Phù hợp, Không phù hợp, Âm tính, Dương tính...
/// </summary>
public class TestResultValue : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã kết quả - Result Code (auto gen: KQddmmyyyyxxx)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên kết quả - Result Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Mô tả - Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected TestResultValue()
    {
    }

    public TestResultValue(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
