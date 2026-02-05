using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Bảng trung gian liên kết Kết quả thử nghiệm với Chỉ tiêu và Nền mẫu (Many-to-Many)
/// Composite key: (TestResultValueId, IndicatorId, SpecimenTypeId)
/// </summary>
public class TestResultValueIndicator : Entity<Guid>, IHasActive
{
    /// <summary>
    /// ID Kết quả thử nghiệm
    /// </summary>
    public Guid TestResultValueId { get; set; }

    /// <summary>
    /// ID Chỉ tiêu
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// ID Nền mẫu (Loại mẫu)
    /// </summary>
    public Guid SpecimenTypeId { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected TestResultValueIndicator()
    {
    }

    public TestResultValueIndicator(
        Guid id,
        Guid testResultValueId,
        Guid indicatorId,
        Guid specimenTypeId) : base(id)
    {
        TestResultValueId = testResultValueId;
        IndicatorId = indicatorId;
        SpecimenTypeId = specimenTypeId;
        IsActive = true;
        SortOrder = 0;
    }
}
