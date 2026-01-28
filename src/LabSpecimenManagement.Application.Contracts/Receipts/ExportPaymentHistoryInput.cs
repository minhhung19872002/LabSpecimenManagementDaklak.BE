using System;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Input cho export danh sách thanh toán
/// </summary>
public class ExportPaymentHistoryInput
{
    /// <summary>
    /// Ngày bắt đầu (filter theo ngày thanh toán)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Ngày kết thúc (filter theo ngày thanh toán)
    /// </summary>
    public DateTime? ToDate { get; set; }
}
