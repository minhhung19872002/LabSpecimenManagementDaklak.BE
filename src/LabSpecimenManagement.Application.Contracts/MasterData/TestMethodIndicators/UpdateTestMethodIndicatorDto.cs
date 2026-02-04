using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class UpdateTestMethodIndicatorDto
{
    /// <summary>
    /// Đơn giá
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải >= 0")]
    public decimal UnitPrice { get; set; }
}
