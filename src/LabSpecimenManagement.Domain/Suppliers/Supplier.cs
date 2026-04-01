using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Suppliers;

/// <summary>
/// Nhà cung cấp
/// </summary>
public class Supplier : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã nhà cung cấp (auto-generated hoặc user tự nhập)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên nhà cung cấp
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Mã số thuế
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Số điện thoại
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Địa chỉ
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Logo file ID (từ File API)
    /// </summary>
    public string? LogoFileId { get; set; }

    /// <summary>
    /// Nhóm nhà cung cấp
    /// </summary>
    public string? SupplierGroup { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Tổng số tiền đã nhập hàng
    /// </summary>
    public decimal TotalPurchase { get; set; }

    /// <summary>
    /// Tổng mua trừ hàng trả
    /// </summary>
    public decimal TotalPurchaseAfterReturn { get; set; }

    /// <summary>
    /// Nợ cần trả hiện tại
    /// </summary>
    public decimal OutstandingDebt { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    protected Supplier()
    {
    }

    public Supplier(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        TotalPurchase = 0;
        TotalPurchaseAfterReturn = 0;
        OutstandingDebt = 0;
        IsActive = true;
    }
}
