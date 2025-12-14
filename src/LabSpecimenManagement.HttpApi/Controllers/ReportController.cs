using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Reports;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Area("app")]
[Route("api/app/report")]
public class ReportController : AbpControllerBase
{
    private readonly IReportAppService _reportAppService;

    public ReportController(IReportAppService reportAppService)
    {
        _reportAppService = reportAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<ReportDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return _reportAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<ReportDto> GetAsync(Guid id)
    {
        return _reportAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual Task<ReportDto> CreateAsync(CreateReportDto input)
    {
        return _reportAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual Task<ReportDto> UpdateAsync(Guid id, UpdateReportDto input)
    {
        return _reportAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _reportAppService.DeleteAsync(id);
    }

    /// <summary>
    /// Generate QR code for the report
    /// </summary>
    [HttpPost("{id}/generate-qr-code")]
    public virtual Task<ReportDto> GenerateQRCodeAsync(Guid id)
    {
        return _reportAppService.GenerateQRCodeAsync(id);
    }

    /// <summary>
    /// Approve the report
    /// </summary>
    [HttpPost("{id}/approve")]
    public virtual Task<ReportDto> ApproveAsync(Guid id)
    {
        return _reportAppService.ApproveAsync(id);
    }

    /// <summary>
    /// Send report via email
    /// </summary>
    [HttpPost("{id}/send")]
    public virtual Task<ReportDto> SendReportAsync(Guid id)
    {
        return _reportAppService.SendReportAsync(id);
    }

    /// <summary>
    /// Cancel the report
    /// </summary>
    [HttpPost("{id}/cancel")]
    public virtual Task<ReportDto> CancelAsync(Guid id)
    {
        return _reportAppService.CancelAsync(id);
    }

    /// <summary>
    /// Export report as PDF
    /// </summary>
    [HttpGet("{id}/export-pdf")]
    public virtual Task<byte[]> ExportToPdfAsync(Guid id)
    {
        return _reportAppService.ExportToPdfAsync(id);
    }

    /// <summary>
    /// Get reports by Test Request ID
    /// </summary>
    [HttpGet("by-test-request/{testRequestId}")]
    public virtual Task<PagedResultDto<ReportDto>> GetByTestRequestIdAsync(
        Guid testRequestId,
        [FromQuery] PagedAndSortedResultRequestDto input)
    {
        return _reportAppService.GetByTestRequestIdAsync(testRequestId, input);
    }
}
