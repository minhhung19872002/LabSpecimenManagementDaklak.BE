using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Co so/Doanh nghiep - Organization
/// </summary>
public class Organization : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Ten co so/doanh nghiep
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Ma tinh thanh
    /// </summary>
    public string ProvinceCode { get; set; } = string.Empty;

    /// <summary>
    /// Ma phuong xa
    /// </summary>
    public string WardCode { get; set; } = string.Empty;

    /// <summary>
    /// Dia chi chi tiet
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Ma so thue
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Nguoi lien he
    /// </summary>
    public string ContactPerson { get; set; } = string.Empty;

    /// <summary>
    /// So dien thoai
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Trang thai hoat dong
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thu tu hien thi
    /// </summary>
    public int SortOrder { get; set; }

    protected Organization()
    {
    }

    public Organization(Guid id, string name) : base(id)
    {
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
