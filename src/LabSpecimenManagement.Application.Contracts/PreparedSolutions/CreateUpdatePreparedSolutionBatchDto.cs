using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.PreparedSolutions;

public class CreateUpdatePreparedSolutionBatchDto
{
    [Required]
    public Guid SolutionProductId { get; set; }

    [Required]
    [MaxLength(256)]
    public string LotCode { get; set; } = string.Empty;

    [Required]
    public DateTime PreparationDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [Range(0.00000001, double.MaxValue)]
    public decimal TotalVolume { get; set; }

    public Guid? StorageWarehouseId { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public string? StorageConditions { get; set; }
    public string? PreparationMethod { get; set; }

    [MinLength(1)]
    public List<CreateIngredientDto> Ingredients { get; set; } = new();
}

public class CreateIngredientDto
{
    public int ProductType { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityUsed { get; set; }
}
