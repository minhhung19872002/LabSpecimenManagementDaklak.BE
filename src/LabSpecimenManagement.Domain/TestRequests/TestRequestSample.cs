using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Thông tin mẫu xét nghiệm trong phiếu yêu cầu
/// </summary>
public class TestRequestSample : FullAuditedEntity<Guid>
{
    /// <summary>
    /// TestRequest ID (Foreign Key)
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// STT (thứ tự trong phiếu)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Mã mẫu
    /// </summary>
    public string SampleCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên mẫu
    /// </summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>
    /// Địa chỉ lấy mẫu
    /// </summary>
    public string? SamplingAddress { get; set; }

    /// <summary>
    /// Lượng mẫu gửi
    /// </summary>
    public string? SampleQuantity { get; set; }

    /// <summary>
    /// Tình trạng mẫu
    /// </summary>
    public SampleStatus Status { get; set; } = SampleStatus.Normal;

    /// <summary>
    /// Tình trạng mẫu (text)
    /// </summary>
    public string? StatusText { get; set; }

    /// <summary>
    /// Trạng thái xử lý (Đã nhận, Đang thử nghiệm, Hoàn thành, Trả kết quả)
    /// </summary>
    public string? ProcessingStatus { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Vị trí lưu mẫu
    /// </summary>
    public string? StorageLocation { get; set; }

    /// <summary>
    /// Tổng chi phí các chỉ tiêu của mẫu này
    /// </summary>
    public decimal TotalIndicatorCost { get; set; }

    /// <summary>
    /// Đánh dấu mẫu đã bị hủy (dùng cho quản lý mẫu lưu kho)
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Ngày hủy mẫu
    /// </summary>
    public DateTime? CancelledDate { get; set; }

    /// <summary>
    /// Đánh dấu mẫu đã bị khách hàng hủy (sau khi đã gửi mẫu)
    /// </summary>
    public bool IsCustomerCancelled { get; set; }

    /// <summary>
    /// Ngày khách hàng hủy mẫu
    /// </summary>
    public DateTime? CustomerCancelledDate { get; set; }

    /// <summary>
    /// Loại mẫu ID (Foreign Key to SpecimenType)
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Đánh dấu mẫu cần hủy (tự động set bởi cronjob khi HĐ hoàn thành + 5 ngày)
    /// </summary>
    public bool NeedDisposal { get; set; }

    /// <summary>
    /// Ngày được đánh dấu cần hủy
    /// </summary>
    public DateTime? NeedDisposalDate { get; set; }

    /// <summary>
    /// Navigation property to TestRequest
    /// </summary>
    public virtual TestRequest TestRequest { get; set; } = null!;

    /// <summary>
    /// Collection of indicators for this sample
    /// </summary>
    public virtual ICollection<TestRequestSampleIndicator> Indicators { get; set; }

    /// <summary>
    /// Collection of attachments (images) for this sample
    /// </summary>
    public virtual ICollection<TestRequestSampleAttachment> Attachments { get; set; }

    protected TestRequestSample()
    {
        Indicators = new List<TestRequestSampleIndicator>();
        Attachments = new List<TestRequestSampleAttachment>();
    }

    public TestRequestSample(Guid id, Guid testRequestId, string sampleCode, string sampleName) : base(id)
    {
        TestRequestId = testRequestId;
        SampleCode = sampleCode;
        SampleName = sampleName;
        Indicators = new List<TestRequestSampleIndicator>();
        Attachments = new List<TestRequestSampleAttachment>();
    }
}
