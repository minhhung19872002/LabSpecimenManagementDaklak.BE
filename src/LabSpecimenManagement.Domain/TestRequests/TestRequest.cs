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
    #region Thông tin hợp đồng

    /// <summary>
    /// Số hợp đồng
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Ngày hợp đồng
    /// </summary>
    public DateTime? ContractDate { get; set; }

    /// <summary>
    /// Gửi kế toán
    /// </summary>
    public bool SentToAccounting { get; set; }

    /// <summary>
    /// Đã thu tiền
    /// </summary>
    public bool PaymentReceived { get; set; }

    /// <summary>
    /// Gửi khoa phòng
    /// </summary>
    public bool SentToDepartment { get; set; }

    /// <summary>
    /// Trả kết quả
    /// </summary>
    public bool ResultReturned { get; set; }

    /// <summary>
    /// Tên cơ sở / Doanh nghiệp
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Mã tỉnh thành
    /// </summary>
    public string? ProvinceCode { get; set; }

    /// <summary>
    /// Tỉnh thành
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    /// Mã quận huyện
    /// </summary>
    public string? DistrictCode { get; set; }

    /// <summary>
    /// Quận huyện
    /// </summary>
    public string? District { get; set; }

    /// <summary>
    /// Mã phường xã
    /// </summary>
    public string? WardCode { get; set; }

    /// <summary>
    /// Phường xã
    /// </summary>
    public string? Ward { get; set; }

    /// <summary>
    /// Địa chỉ chi tiết
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Mã số thuế
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Người liên hệ
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Số điện thoại
    /// </summary>
    public string? Phone { get; set; }

    #endregion

    #region Thông tin còn lại

    /// <summary>
    /// Tổng số mẫu gửi
    /// </summary>
    public int TotalSampleCount { get; set; }

    #endregion

    #region Phương pháp thử nghiệm

    /// <summary>
    /// Viện VS DT Tây Nguyên
    /// </summary>
    public bool MethodWestHighlandInstitute { get; set; }

    /// <summary>
    /// Khách hàng (checkbox)
    /// </summary>
    public bool MethodCustomer { get; set; }

    /// <summary>
    /// Khách hàng - chi tiết
    /// </summary>
    public string? MethodCustomerDetail { get; set; }

    /// <summary>
    /// Phiếu kết quả thử nghiệm - Ngôn ngữ
    /// </summary>
    public ResultLanguage ResultLanguage { get; set; } = ResultLanguage.Vietnamese;

    /// <summary>
    /// Số bản KQTN yêu cầu
    /// </summary>
    public string? ResultCopyCount { get; set; }

    /// <summary>
    /// Yêu cầu tách KQ đạt
    /// </summary>
    public bool SeparatePassedResults { get; set; }

    /// <summary>
    /// Công bố về sự phù hợp với
    /// </summary>
    public string? ConformityDeclaration { get; set; }

    /// <summary>
    /// Yêu cầu khác
    /// </summary>
    public string? OtherRequirements { get; set; }

    /// <summary>
    /// Hợp đồng phụ
    /// </summary>
    public string? SubContract { get; set; }

    /// <summary>
    /// Yêu cầu về lấy mẫu
    /// </summary>
    public SamplingRequirement SamplingRequirement { get; set; } = SamplingRequirement.CustomerSampling;

    /// <summary>
    /// Hình thức trả kết quả - Khách hàng nhận trực tiếp
    /// </summary>
    public bool DeliveryDirectPickup { get; set; }

    /// <summary>
    /// Hình thức trả kết quả - Gửi bưu điện
    /// </summary>
    public bool DeliveryPostal { get; set; }

    /// <summary>
    /// Trả lại mẫu lưu
    /// </summary>
    public bool ReturnStoredSample { get; set; }

    /// <summary>
    /// Hình thức mẫu gửi - Dịch vụ
    /// </summary>
    public bool SampleTypeService { get; set; }

    /// <summary>
    /// Hình thức mẫu gửi - Hợp đồng
    /// </summary>
    public bool SampleTypeContract { get; set; }

    /// <summary>
    /// Hình thức mẫu gửi - Mẫu nhiệm vụ
    /// </summary>
    public bool SampleTypeTask { get; set; }

    /// <summary>
    /// Hình thức mẫu gửi - Hợp đồng phụ
    /// </summary>
    public bool SampleTypeSubContract { get; set; }

    /// <summary>
    /// Ngày trả kết quả
    /// </summary>
    public DateTime? ResultDeliveryDate { get; set; }

    /// <summary>
    /// Lưu mẫu
    /// </summary>
    public SampleStorageOption SampleStorage { get; set; } = SampleStorageOption.No;

    /// <summary>
    /// Tổng chi phí
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Bằng chữ
    /// </summary>
    public string? TotalCostInWords { get; set; }

    /// <summary>
    /// Số phiếu thu (TTDV)
    /// </summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Phụ trách (UserId)
    /// </summary>
    public Guid? SupervisorId { get; set; }

    /// <summary>
    /// GĐ Trung tâm (UserId)
    /// </summary>
    public Guid? DirectorId { get; set; }

    /// <summary>
    /// Thay đổi yêu cầu
    /// </summary>
    public string? ChangeRequest { get; set; }

    /// <summary>
    /// Đã đọc và đồng ý với quy định chung
    /// </summary>
    public bool AgreeTerms { get; set; }

    #endregion

    #region Các fields cũ giữ lại để tương thích

    /// <summary>
    /// Số phiếu yêu cầu - Request Number (auto-generated)
    /// </summary>
    public string RequestNumber { get; set; } = string.Empty;

    /// <summary>
    /// Ngày yêu cầu - Request Date
    /// </summary>
    public DateTime RequestDate { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Trạng thái - Status
    /// </summary>
    public TestRequestStatus Status { get; set; }

    /// <summary>
    /// Lý do từ chối kết quả (khi QLCL từ chối)
    /// </summary>
    public string? ResultRejectionReason { get; set; }

    /// <summary>
    /// Thời gian hoàn thành dự kiến
    /// </summary>
    public DateTime? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Nhà thầu phụ (nếu có)
    /// </summary>
    public string? Subcontractor { get; set; }

    /// <summary>
    /// Ngày gửi mẫu (ngày lưu kho) - được set khi status chuyển sang SampleSent
    /// </summary>
    public DateTime? SampleSentDate { get; set; }

    #endregion

    #region Collections

    /// <summary>
    /// Collection of samples in this request
    /// </summary>
    public virtual ICollection<TestRequestSample> Samples { get; set; }

    /// <summary>
    /// Collection of specimens in this request (legacy)
    /// </summary>
    public virtual ICollection<Specimen> Specimens { get; set; }

    /// <summary>
    /// Collection of attachments
    /// </summary>
    public virtual ICollection<TestRequestAttachment> Attachments { get; set; }

    #endregion

    protected TestRequest()
    {
        Samples = new List<TestRequestSample>();
        Specimens = new List<Specimen>();
        Attachments = new List<TestRequestAttachment>();
    }

    public TestRequest(Guid id, string requestNumber, DateTime requestDate, string customerName) : base(id)
    {
        RequestNumber = requestNumber;
        RequestDate = requestDate;
        CustomerName = customerName;
        Status = TestRequestStatus.New;
        Samples = new List<TestRequestSample>();
        Specimens = new List<Specimen>();
        Attachments = new List<TestRequestAttachment>();
    }
}
