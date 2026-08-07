using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Common;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.StockAdjustments;

public class StockAdjustmentListDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string? CreatorName { get; set; }
    public StockAdjustmentStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime InventoryDate { get; set; }
    public string? InventoryUserName { get; set; }
}

public class StockAdjustmentDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string? CreatorName { get; set; }
    public StockAdjustmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime InventoryDate { get; set; }
    public Guid? InventoryUserId { get; set; }
    public string? InventoryUserName { get; set; }
    public List<StockAdjustmentItemDto> Items { get; set; } = new();
}

public class StockAdjustmentItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal QuantityBefore { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? ActualUnitId { get; set; }
    public string? ActualUnitName { get; set; }
    public string? Reason { get; set; }
    public decimal? ExpectedStock { get; set; }
}

public class CreateUpdateStockAdjustmentDto
{
    [StringLength(2000)]
    public string? Notes { get; set; }

    public DateTime? InventoryDate { get; set; }

    [Required]
    public List<CreateUpdateStockAdjustmentItemDto> Items { get; set; } = new();
}

public class CreateUpdateStockAdjustmentItemDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    public decimal ActualQuantity { get; set; }

    public Guid? WarehouseId { get; set; }

    public Guid? ActualUnitId { get; set; }

    [StringLength(1000)]
    public string? Reason { get; set; }

    public decimal? ExpectedStock { get; set; }
}

public class GetStockAdjustmentListInput : PagedRequestDto
{
    public string? Search { get; set; }
    public StockAdjustmentStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class ConfirmPreviewDto
{
    public bool HasChanges { get; set; }
    public List<ConfirmPreviewItemDto> Items { get; set; } = new();
}

public class ConfirmPreviewItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal SnapshotQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public bool HasChanged { get; set; }
}
