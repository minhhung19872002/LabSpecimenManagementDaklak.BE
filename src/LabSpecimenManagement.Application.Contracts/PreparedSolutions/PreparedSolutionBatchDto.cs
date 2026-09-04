using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.PreparedSolutions;

public class PreparedSolutionBatchDto : AuditedEntityDto<Guid>
{
    public Guid SolutionProductId { get; set; }
    public string SolutionProductName { get; set; } = string.Empty;
    public string? IdentificationCode { get; set; }
    public string LotCode { get; set; } = string.Empty;
    public DateTime PreparationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal RemainingVolume { get; set; }
    public Guid? VolumeUnitId { get; set; }
    public string? VolumeUnitName { get; set; }
    public Guid? StorageWarehouseId { get; set; }
    public string? StorageWarehouseName { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public string? SourceWarehouseName { get; set; }
    public string? StorageConditions { get; set; }
    public string? PreparationMethod { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessingNote { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public List<string> ProcessingImageUrls { get; set; } = new();
    public string? Concentration { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public string? PreparedByName { get; set; }

    // Computed server-side on every response
    public PreparedSolutionStatus ComputedStatus { get; set; }
    /// <summary>Phiếu nhập kho ảo (PSB-xxx) sinh ra khi pha — chỉ điền ở GetAsync.</summary>
    public Guid? PreparedSolutionReceiptId { get; set; }
    public string? PreparedSolutionReceiptCode { get; set; }
    public int DaysRemaining { get; set; }

    public List<PreparedSolutionIngredientDto> Ingredients { get; set; } = new();
}
