using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Input DTO cho danh sách phiếu thu
/// </summary>
public class GetReceiptListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Tìm kiếm theo số hợp đồng, tên khách hàng, số phiếu thu
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Lọc theo trạng thái
    /// </summary>
    public ReceiptStatus? Status { get; set; }
}
