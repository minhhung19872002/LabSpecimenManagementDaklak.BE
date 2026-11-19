using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Bảng trung gian liên kết Phương pháp thử và Chỉ tiêu (Many-to-Many)
/// </summary>
public class TestMethodIndicator : Entity<Guid>, ISoftDelete
{
    /// <summary>
    /// ID Phương pháp thử
    /// </summary>
    public Guid TestMethodId { get; set; }

    /// <summary>
    /// ID Chỉ tiêu
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Đơn giá riêng cho chỉ tiêu trong phương pháp này.
    /// null = "Chờ định giá" (khai báo dịch vụ chưa nhập giá) — luồng phiếu / báo giá coi như 0.
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Nền mẫu (Loại mẫu) - cho phép cùng indicator thêm nhiều lần với nền mẫu khác nhau
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Đơn vị tính riêng cho chỉ tiêu trong phương pháp này (override TestMethod.Unit nếu có)
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Đơn vị tính có định dạng HTML
    /// </summary>
    public string? FormattedUnit { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong phương pháp
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Trạng thái kích hoạt (khi ngừng thì không hiển thị trên phiếu thử nghiệm)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Cờ HĐ phụ (hợp đồng phụ / subcontract)
    /// </summary>
    public bool IsSubcontract { get; set; }

    /// <summary>
    /// Đơn vị nhận thầu (danh mục Nhà thầu phụ) khi là HĐ phụ — không bắt buộc
    /// </summary>
    public Guid? SubcontractorId { get; set; }

    /// <summary>
    /// Mã dịch vụ (DV0001...) — L2 Dịch vụ & Bảng giá. Sinh tự động bằng sequence DB.
    /// </summary>
    public string? ServiceCode { get; set; }

    /// <summary>
    /// Thời gian trả kết quả (ngày) của version giá đang áp dụng
    /// </summary>
    public int? TurnaroundDays { get; set; }

    /// <summary>
    /// Xóa mềm: phiếu đã dùng dịch vụ giữ snapshot, dịch vụ ẩn khỏi danh mục
    /// </summary>
    public bool IsDeleted { get; set; }

    protected TestMethodIndicator()
    {
    }

    public TestMethodIndicator(Guid id, Guid testMethodId, Guid indicatorId, decimal? unitPrice, Guid? specimenTypeId = null, string? unit = null, string? formattedUnit = null, bool isActive = true) : base(id)
    {
        TestMethodId = testMethodId;
        IndicatorId = indicatorId;
        UnitPrice = unitPrice;
        SpecimenTypeId = specimenTypeId;
        Unit = unit;
        FormattedUnit = formattedUnit;
        SortOrder = 0;
        IsActive = isActive;
    }
}
