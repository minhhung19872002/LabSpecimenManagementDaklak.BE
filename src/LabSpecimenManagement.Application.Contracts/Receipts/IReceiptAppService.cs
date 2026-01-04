using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Interface cho Receipt Application Service
/// </summary>
public interface IReceiptAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách phiếu thu với phân trang và filter
    /// </summary>
    Task<PagedResultDto<ReceiptDto>> GetListAsync(GetReceiptListInput input);

    /// <summary>
    /// Lấy chi tiết phiếu thu theo ID (basic)
    /// </summary>
    Task<ReceiptDto> GetAsync(Guid id);

    /// <summary>
    /// Lấy chi tiết phiếu thu đầy đủ (bao gồm TestRequest với samples, indicators)
    /// </summary>
    Task<ReceiptDetailDto> GetDetailAsync(Guid id);

    /// <summary>
    /// Cập nhật thông tin phiếu thu (số phiếu thu, mã số biên lai)
    /// </summary>
    Task UpdateReceiptInfoAsync(Guid id, UpdateReceiptDto input);

    /// <summary>
    /// Đánh dấu đã thanh toán
    /// </summary>
    Task MarkAsPaidAsync(Guid id);

    /// <summary>
    /// Đánh dấu chưa thanh toán
    /// </summary>
    Task MarkAsUnpaidAsync(Guid id);
}
