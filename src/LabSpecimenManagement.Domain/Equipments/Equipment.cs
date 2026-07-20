using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Equipments;

public class Equipment : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public EquipmentStatus Status { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string? GoodsReceiptCode { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Origin { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal Price { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public int UsageCount { get; set; }
    public int MaintenanceCount { get; set; }
    public string? Notes { get; set; }
    public string? AssetCode { get; set; }
    public int? DepreciationPeriod { get; set; }
    public string? Location { get; set; }
    public decimal? PowerConsumption { get; set; }
    public decimal? OperatingHoursPerYear { get; set; }
    public int? AverageSamplesPerYear { get; set; }
    public decimal? MaintenanceCostRatio { get; set; }
    public decimal? CalibrationCostPerYear { get; set; }
    public DateTime? ImportDate { get; set; }

    protected Equipment() { }

    public Equipment(Guid id, string code, string name, string serialNumber) : base(id)
    {
        Code = code;
        Name = name;
        SerialNumber = serialNumber;
        Status = EquipmentStatus.InStock;
    }
}
