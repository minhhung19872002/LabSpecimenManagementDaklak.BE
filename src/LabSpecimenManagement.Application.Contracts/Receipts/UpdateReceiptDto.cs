using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// DTO để cập nhật thông tin phiếu thu
/// </summary>
public class UpdateReceiptDto
{
    /// <summary>
    /// Số phiếu thu (chỉ số nguyên)
    /// </summary>
    [RegularExpression(@"^[0-9]*$", ErrorMessage = "Số phiếu thu chỉ được chứa số")]
    [MaxLength(50)]
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Mã số biên lai (chỉ số nguyên)
    /// </summary>
    [RegularExpression(@"^[0-9]*$", ErrorMessage = "Mã số biên lai chỉ được chứa số")]
    [MaxLength(50)]
    public string? VoucherCode { get; set; }
}
