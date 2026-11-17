using System;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Bảng trung gian liên kết Sample với Indicator
/// </summary>
public class TestRequestSampleIndicator : Entity<Guid>
{
    /// <summary>
    /// TestRequestSample ID (Foreign Key)
    /// </summary>
    public Guid TestRequestSampleId { get; set; }

    /// <summary>
    /// Indicator ID (Foreign Key)
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Laboratory ID - Khoa phòng thực hiện (Foreign Key)
    /// </summary>
    public Guid? LaboratoryId { get; set; }

    /// <summary>
    /// Test Method ID - Phương pháp thử (Foreign Key)
    /// </summary>
    public Guid? TestMethodId { get; set; }

    /// <summary>
    /// Specimen Type ID - Nền mẫu (Foreign Key)
    /// Lưu để phân biệt giá của cùng indicator+method ở các nền mẫu khác nhau
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Đơn giá tại thời điểm tạo (snapshot)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Giá gốc trước khi áp dụng combo giảm giá (chỉ có khi được miễn phí do combo kim loại nặng)
    /// </summary>
    public decimal? OriginalUnitPrice { get; set; }

    /// <summary>
    /// Ghi chú cho chỉ tiêu này
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Ngày chuyển giao
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày yêu cầu
    /// </summary>
    public DateTime? RequestDate { get; set; }

    /// <summary>
    /// Đã gửi khoa phòng
    /// </summary>
    public bool SentToDepartment { get; set; }

    /// <summary>
    /// Hợp đồng phụ (snapshot từ TestMethodIndicator.IsSubcontract tại thời điểm tạo)
    /// </summary>
    public bool IsSubcontract { get; set; }

    #region Snapshot master data (giữ nguyên khi danh mục bị sửa/xóa)

    /// <summary>
    /// Snapshot mã chỉ tiêu
    /// </summary>
    public string? IndicatorCode { get; set; }

    /// <summary>
    /// Snapshot tên chỉ tiêu
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// Snapshot tên chỉ tiêu có định dạng (HTML)
    /// </summary>
    public string? IndicatorFormattedName { get; set; }

    /// <summary>
    /// Snapshot tên nhóm chỉ tiêu
    /// </summary>
    public string? IndicatorGroupName { get; set; }

    /// <summary>
    /// Snapshot mã phương pháp thử
    /// </summary>
    public string? TestMethodCode { get; set; }

    /// <summary>
    /// Snapshot tên phương pháp thử
    /// </summary>
    public string? TestMethodName { get; set; }

    /// <summary>
    /// Snapshot tên phương pháp thử có định dạng (HTML)
    /// </summary>
    public string? TestMethodFormattedName { get; set; }

    /// <summary>
    /// Snapshot đơn vị tính của phương pháp
    /// </summary>
    public string? TestMethodUnit { get; set; }

    /// <summary>
    /// Snapshot đơn vị tính có định dạng (HTML)
    /// </summary>
    public string? TestMethodFormattedUnit { get; set; }

    /// <summary>
    /// Snapshot tên khoa phòng thực hiện (tên phòng ban của Laboratory)
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Snapshot tên loại mẫu của chỉ tiêu
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    #endregion

    /// <summary>
    /// Navigation property to TestRequestSample
    /// </summary>
    public virtual TestRequestSample TestRequestSample { get; set; } = null!;

    /// <summary>
    /// Navigation property to Indicator
    /// </summary>
    public virtual Indicator Indicator { get; set; } = null!;

    /// <summary>
    /// Navigation property to Laboratory
    /// </summary>
    public virtual Laboratory? Laboratory { get; set; }

    /// <summary>
    /// Navigation property to TestMethod
    /// </summary>
    public virtual TestMethod? TestMethod { get; set; }

    protected TestRequestSampleIndicator()
    {
    }

    public TestRequestSampleIndicator(Guid id, Guid testRequestSampleId, Guid indicatorId) : base(id)
    {
        TestRequestSampleId = testRequestSampleId;
        IndicatorId = indicatorId;
    }
}
