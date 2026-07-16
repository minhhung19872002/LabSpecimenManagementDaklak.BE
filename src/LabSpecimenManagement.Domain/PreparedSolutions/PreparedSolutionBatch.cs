using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.PreparedSolutions;

public class PreparedSolutionBatch : FullAuditedAggregateRoot<Guid>
{
    // Solution product from master data (ProductType.PreparedSolution = 6)
    public Guid SolutionProductId { get; set; }
    public string SolutionProductName { get; set; } = string.Empty;
    public string? IdentificationCode { get; set; }

    // Lot / batch identity
    public string LotCode { get; set; } = string.Empty;

    // Dates
    public DateTime PreparationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    // Volume
    public decimal TotalVolume { get; set; }
    public decimal RemainingVolume { get; set; }
    public Guid? VolumeUnitId { get; set; }
    public string? VolumeUnitName { get; set; }

    // Storage location (Warehouse Lv1)
    public Guid? StorageWarehouseId { get; set; }
    public string? StorageWarehouseName { get; set; }

    // Source warehouse for ingredients (Warehouse Lv0)
    public Guid? SourceWarehouseId { get; set; }
    public string? SourceWarehouseName { get; set; }

    // Free-text fields (stored as HTML from rich text editor, toolbar disabled)
    public string? StorageConditions { get; set; }
    public string? PreparationMethod { get; set; }

    // Processing / disposal
    public bool IsProcessed { get; set; }
    public string? ProcessingNote { get; set; }
    public DateTime? ProcessedAt { get; set; }
    // JSON string[] of file URLs attached during processing
    public string ProcessingImages { get; set; } = "[]";

    // Denormalized from product at creation time
    public string? Concentration { get; set; }

    // Denormalized audit: who prepared
    public Guid? PreparedByUserId { get; set; }
    public string? PreparedByName { get; set; }

    public ICollection<PreparedSolutionIngredient> Ingredients { get; set; } = new List<PreparedSolutionIngredient>();
}
