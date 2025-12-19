using System;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Bảng trung gian liên kết LaboratoryTask với TestRequestSampleIndicator
/// Bao gồm thông tin gán phương pháp, kỹ thuật viên, kết quả và duyệt
/// </summary>
public class LaboratoryTaskIndicator : Entity<Guid>
{
    /// <summary>
    /// LaboratoryTask ID (Foreign Key)
    /// </summary>
    public Guid LaboratoryTaskId { get; set; }

    /// <summary>
    /// TestRequestSampleIndicator ID (Foreign Key)
    /// </summary>
    public Guid TestRequestSampleIndicatorId { get; set; }

    /// <summary>
    /// Phương pháp thử nghiệm - Method ID
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>
    /// Kỹ thuật viên thực hiện - Technician ID
    /// </summary>
    public Guid? TechnicianId { get; set; }

    /// <summary>
    /// Kết quả thí nghiệm - Result
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Đơn vị tính - Unit
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Ngày nhập kết quả - Entry Date
    /// </summary>
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// Người nhập kết quả - Entered By User ID
    /// </summary>
    public Guid? EnteredById { get; set; }

    /// <summary>
    /// Trạng thái duyệt - Approval Status
    /// </summary>
    public IndicatorApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Ngày duyệt - Approval Date
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Người duyệt - Approved By User ID
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Lý do từ chối - Rejection Reason
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Navigation property to LaboratoryTask
    /// </summary>
    public virtual LaboratoryTask LaboratoryTask { get; set; } = null!;

    /// <summary>
    /// Navigation property to TestRequestSampleIndicator
    /// </summary>
    public virtual TestRequestSampleIndicator TestRequestSampleIndicator { get; set; } = null!;

    protected LaboratoryTaskIndicator()
    {
    }

    public LaboratoryTaskIndicator(
        Guid id,
        Guid laboratoryTaskId,
        Guid testRequestSampleIndicatorId) : base(id)
    {
        LaboratoryTaskId = laboratoryTaskId;
        TestRequestSampleIndicatorId = testRequestSampleIndicatorId;
        ApprovalStatus = IndicatorApprovalStatus.Pending;
    }
}
