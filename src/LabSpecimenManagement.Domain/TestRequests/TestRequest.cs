using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Phiếu yêu cầu thử nghiệm - Test Request
/// ISO/IEC 17025:2017 compliant
/// </summary>
public class TestRequest : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Số phiếu yêu cầu - Request Number (auto-generated)
    /// </summary>
    public string RequestNumber { get; set; }

    /// <summary>
    /// Ngày yêu cầu - Request Date
    /// </summary>
    public DateTime RequestDate { get; set; }

    /// <summary>
    /// Tên khách hàng - Customer Name
    /// </summary>
    public string CustomerName { get; set; }

    /// <summary>
    /// Thông tin khách hàng - Customer Info (contact, address, etc.)
    /// </summary>
    public string? CustomerInfo { get; set; }

    /// <summary>
    /// Email khách hàng - Customer Email
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Số điện thoại - Customer Phone
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Trạng thái - Status (Pending, InProgress, Completed, Cancelled)
    /// </summary>
    public TestRequestStatus Status { get; set; }

    /// <summary>
    /// Thời gian hoàn thành dự kiến - Expected completion date (TAT calculation)
    /// </summary>
    public DateTime? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Nhà thầu phụ (nếu có) - Subcontractor
    /// </summary>
    public string? Subcontractor { get; set; }

    /// <summary>
    /// Collection of specimens in this request
    /// </summary>
    public virtual ICollection<Specimen> Specimens { get; set; }

    /// <summary>
    /// Collection of attachments (hồ sơ, biên bản)
    /// </summary>
    public virtual ICollection<TestRequestAttachment> Attachments { get; set; }

    protected TestRequest()
    {
        Specimens = new List<Specimen>();
        Attachments = new List<TestRequestAttachment>();
    }

    public TestRequest(
        Guid id,
        string requestNumber,
        DateTime requestDate,
        string customerName,
        string? customerEmail,
        string? customerPhone
    ) : base(id)
    {
        RequestNumber = requestNumber;
        RequestDate = requestDate;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        CustomerPhone = customerPhone;
        Status = TestRequestStatus.Pending;
        Specimens = new List<Specimen>();
        Attachments = new List<TestRequestAttachment>();
    }
}
