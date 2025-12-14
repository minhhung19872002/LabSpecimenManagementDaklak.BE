using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Reports;

public interface IReportAppService :
    ICrudAppService<
        ReportDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateReportDto,
        UpdateReportDto>
{
    /// <summary>
    /// Generate QR code for the report
    /// </summary>
    Task<ReportDto> GenerateQRCodeAsync(Guid id);

    /// <summary>
    /// Approve the report
    /// </summary>
    Task<ReportDto> ApproveAsync(Guid id);

    /// <summary>
    /// Send report via email
    /// </summary>
    Task<ReportDto> SendReportAsync(Guid id);

    /// <summary>
    /// Cancel the report
    /// </summary>
    Task<ReportDto> CancelAsync(Guid id);

    /// <summary>
    /// Export report as PDF
    /// </summary>
    Task<byte[]> ExportToPdfAsync(Guid id);

    /// <summary>
    /// Get reports by Test Request ID
    /// </summary>
    Task<PagedResultDto<ReportDto>> GetByTestRequestIdAsync(Guid testRequestId, PagedAndSortedResultRequestDto input);
}
