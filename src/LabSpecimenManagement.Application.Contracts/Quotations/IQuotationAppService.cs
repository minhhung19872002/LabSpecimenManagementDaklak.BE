using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// Interface cho Quotation Application Service
/// </summary>
public interface IQuotationAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách báo giá có phân trang
    /// </summary>
    Task<PagedResultDto<QuotationListDto>> GetListAsync(GetQuotationListInput input);

    /// <summary>
    /// Lấy chi tiết báo giá
    /// </summary>
    Task<QuotationDto> GetAsync(Guid id);

    /// <summary>
    /// Tạo mới báo giá
    /// </summary>
    Task<QuotationDto> CreateAsync(CreateUpdateQuotationDto input);

    /// <summary>
    /// Cập nhật báo giá
    /// </summary>
    Task<QuotationDto> UpdateAsync(Guid id, CreateUpdateQuotationDto input);

    /// <summary>
    /// Xóa báo giá
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Sinh mã báo giá mới
    /// </summary>
    Task<string> GenerateQuotationNumberAsync();

    /// <summary>
    /// Gửi email báo giá với PDF đính kèm
    /// </summary>
    Task SendEmailAsync(SendQuotationEmailDto input);

    /// <summary>
    /// Tạo PDF báo giá để preview
    /// </summary>
    Task<byte[]> GeneratePdfAsync(GenerateQuotationPdfDto input);
}
