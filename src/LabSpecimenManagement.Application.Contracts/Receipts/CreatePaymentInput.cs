using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Input để tạo thanh toán mới
/// </summary>
public class CreatePaymentInput
{
    /// <summary>
    /// Số tiền thanh toán (phải > 0)
    /// </summary>
    [Required(ErrorMessage = "Số tiền thanh toán là bắt buộc")]
    [Range(1, double.MaxValue, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Phương thức thanh toán
    /// </summary>
    [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
    public string? Notes { get; set; }
}
