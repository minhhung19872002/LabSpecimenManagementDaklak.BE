using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// Input filter/search danh sách báo giá
/// </summary>
public class GetQuotationListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Tìm theo mã báo giá hoặc tên khách hàng
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Lọc theo trạng thái
    /// </summary>
    public QuotationStatus? Status { get; set; }

    /// <summary>
    /// Lọc từ ngày
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Lọc đến ngày
    /// </summary>
    public DateTime? ToDate { get; set; }
}
