using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Receipts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// Controller cho quản lý phiếu thu
/// </summary>
[RemoteService]
[Area("app")]
[Route("api/v1/receipts")]
public class ReceiptController : LabSpecimenManagementController
{
    private readonly IReceiptAppService _receiptAppService;

    public ReceiptController(IReceiptAppService receiptAppService)
    {
        _receiptAppService = receiptAppService;
    }

    /// <summary>
    /// Lấy danh sách phiếu thu với phân trang và filter
    /// </summary>
    [HttpGet]
    public async Task<PagedResultDto<ReceiptDto>> GetListAsync([FromQuery] GetReceiptListInput input)
    {
        return await _receiptAppService.GetListAsync(input);
    }

    /// <summary>
    /// Lấy chi tiết phiếu thu theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ReceiptDto> GetAsync(Guid id)
    {
        return await _receiptAppService.GetAsync(id);
    }

    /// <summary>
    /// Lấy chi tiết phiếu thu đầy đủ (bao gồm TestRequest với samples, indicators)
    /// </summary>
    [HttpGet("{id}/detail")]
    public async Task<ReceiptDetailDto> GetDetailAsync(Guid id)
    {
        return await _receiptAppService.GetDetailAsync(id);
    }

    /// <summary>
    /// Cập nhật thông tin phiếu thu (số phiếu thu, mã số biên lai)
    /// </summary>
    [HttpPut("{id}")]
    public async Task UpdateReceiptInfoAsync(Guid id, [FromBody] UpdateReceiptDto input)
    {
        await _receiptAppService.UpdateReceiptInfoAsync(id, input);
    }

    /// <summary>
    /// Đánh dấu đã thanh toán
    /// </summary>
    [HttpPost("{id}/mark-paid")]
    public async Task MarkAsPaidAsync(Guid id)
    {
        await _receiptAppService.MarkAsPaidAsync(id);
    }

    /// <summary>
    /// Đánh dấu chưa thanh toán
    /// </summary>
    [HttpPost("{id}/mark-unpaid")]
    public async Task MarkAsUnpaidAsync(Guid id)
    {
        await _receiptAppService.MarkAsUnpaidAsync(id);
    }
}
