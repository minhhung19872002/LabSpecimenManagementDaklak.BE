using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    /// Ngày giao việc cho kỹ thuật viên - Assigned Date
    /// </summary>
    public DateTime? AssignedDate { get; set; }

    /// <summary>
    /// Ngày thử nghiệm - Test Date (ngày KTV bắt đầu thử nghiệm/check-in)
    /// </summary>
    public DateTime? TestDate { get; set; }

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
    /// Ngày gửi kết quả cho QLKT - Submit Date
    /// </summary>
    public DateTime? SubmitDate { get; set; }

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
    /// Lý do từ chối (kết quả không đạt) - Rejection Reason
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Lý do từ chối thử nghiệm (QLKT từ chối trong giai đoạn đánh giá) - Testing Rejection Reason
    /// </summary>
    [MaxLength(1000)]
    public string? TestingRejectionReason { get; set; }

    /// <summary>
    /// Ngày QLKT từ chối thử nghiệm
    /// </summary>
    public DateTime? TestingRejectionDate { get; set; }

    /// <summary>
    /// Người từ chối thử nghiệm - QLKT User ID
    /// </summary>
    public Guid? TestingRejectedById { get; set; }

    /// <summary>
    /// Tên người từ chối thử nghiệm
    /// </summary>
    [MaxLength(256)]
    public string? TestingRejectedByName { get; set; }

    /// <summary>
    /// Ngày KH xác nhận (đổi PP hoặc từ chối TN)
    /// </summary>
    public DateTime? CustomerConfirmationDate { get; set; }

    /// <summary>
    /// Người xác nhận KH - BPGN User ID
    /// </summary>
    public Guid? CustomerConfirmedById { get; set; }

    /// <summary>
    /// Tên người xác nhận KH
    /// </summary>
    [MaxLength(256)]
    public string? CustomerConfirmedByName { get; set; }

    /// <summary>
    /// Nội dung yêu cầu bổ sung (QLKT yêu cầu KH cung cấp)
    /// </summary>
    [MaxLength(2000)]
    public string? SupplementaryRequestReason { get; set; }

    /// <summary>
    /// Ngày QLKT yêu cầu bổ sung
    /// </summary>
    public DateTime? SupplementaryRequestDate { get; set; }

    /// <summary>
    /// Người yêu cầu bổ sung - QLKT User ID
    /// </summary>
    public Guid? SupplementaryRequestedById { get; set; }

    /// <summary>
    /// Tên người yêu cầu bổ sung
    /// </summary>
    [MaxLength(256)]
    public string? SupplementaryRequestedByName { get; set; }

    /// <summary>
    /// Đã gửi cho QLCL chưa - Is Sent To Quality
    /// </summary>
    public bool IsSentToQuality { get; set; }

    /// <summary>
    /// Tiêu chuẩn so sánh - Standard ID (để so sánh kết quả với giới hạn cho phép)
    /// </summary>
    public Guid? StandardId { get; set; }

    /// <summary>
    /// Ghi chú của kỹ thuật viên khi nhập kết quả
    /// </summary>
    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>
    /// Navigation property to LaboratoryTask
    /// </summary>
    public virtual LaboratoryTask LaboratoryTask { get; set; } = null!;

    /// <summary>
    /// Navigation property to TestRequestSampleIndicator
    /// </summary>
    public virtual TestRequestSampleIndicator TestRequestSampleIndicator { get; set; } = null!;

    /// <summary>
    /// Lịch sử từ chối - Rejection Histories
    /// </summary>
    public virtual ICollection<IndicatorRejectionHistory> RejectionHistories { get; set; } = new List<IndicatorRejectionHistory>();

    /// <summary>
    /// Lịch sử yêu cầu đổi phương pháp - Method Change Histories
    /// </summary>
    public virtual ICollection<MethodChangeHistory> MethodChangeHistories { get; set; } = new List<MethodChangeHistory>();

    /// <summary>
    /// Hình ảnh của chỉ tiêu - Indicator Images
    /// </summary>
    public virtual ICollection<IndicatorImage> Images { get; set; } = new List<IndicatorImage>();

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
