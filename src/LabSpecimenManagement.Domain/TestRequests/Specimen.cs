using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Mẫu thử nghiệm - Specimen
/// ISO/IEC 17025:2017 - Section 7.4 Handling of test items
/// </summary>
public class Specimen : FullAuditedAggregateRoot<Guid>
{
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Mã mẫu - Specimen Code (auto-generated)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên mẫu - Specimen Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Loại mẫu - Specimen Type
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Ngày tiếp nhận - Received Date
    /// </summary>
    public DateTime ReceivedDate { get; set; }

    /// <summary>
    /// Trạng thái mẫu - Status (Received, Testing, Storage, Disposal, Destroyed, ReturnedToCustomer)
    /// </summary>
    public SpecimenStatus Status { get; set; }

    /// <summary>
    /// Vị trí lưu mẫu - Storage Location
    /// </summary>
    public string? StorageLocation { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Hình ảnh mẫu - Specimen images
    /// </summary>
    public string? Images { get; set; }

    /// <summary>
    /// Sai lệch (deviation) nếu có
    /// </summary>
    public string? Deviation { get; set; }

    /// <summary>
    /// Số lượng mẫu
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Đơn vị
    /// </summary>
    public string? Unit { get; set; }

    public virtual TestRequest TestRequest { get; set; }

    /// <summary>
    /// Collection of tests to be performed on this specimen
    /// </summary>
    public virtual ICollection<Test> Tests { get; set; }

    /// <summary>
    /// Lịch sử đường đi của mẫu - Specimen history tracking
    /// </summary>
    public virtual ICollection<SpecimenHistory> History { get; set; }

    protected Specimen()
    {
        Tests = new List<Test>();
        History = new List<SpecimenHistory>();
    }

    public Specimen(
        Guid id,
        Guid testRequestId,
        string code,
        string name,
        string type,
        DateTime receivedDate
    ) : base(id)
    {
        TestRequestId = testRequestId;
        Code = code;
        Name = name;
        Type = type;
        ReceivedDate = receivedDate;
        Status = SpecimenStatus.Received;
        Tests = new List<Test>();
        History = new List<SpecimenHistory>();
    }
}
