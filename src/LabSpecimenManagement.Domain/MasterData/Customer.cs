using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Khách hàng - Customer
/// </summary>
public class Customer : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã khách hàng - Customer Code (auto-generated)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên khách hàng - Customer Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Số điện thoại
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Địa chỉ
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
    /// Số điện thoại người liên hệ
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    protected Customer()
    {
    }

    public Customer(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }
}
