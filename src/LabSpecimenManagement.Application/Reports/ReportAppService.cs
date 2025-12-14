using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using QRCoder;

namespace LabSpecimenManagement.Reports;

[RemoteService(IsEnabled = false)]
public class ReportAppService :
    CrudAppService<
        Report,
        ReportDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateReportDto,
        UpdateReportDto>,
    IReportAppService
{
    private readonly IRepository<ReportTestResult> _reportTestResultRepository;

    public ReportAppService(
        IRepository<Report, Guid> repository,
        IRepository<ReportTestResult> reportTestResultRepository)
        : base(repository)
    {
        _reportTestResultRepository = reportTestResultRepository;
    }

    protected override async Task<Report> MapToEntityAsync(CreateReportDto createInput)
    {
        // Generate report number automatically
        var reportNumber = await GenerateReportNumberAsync();

        var entity = new Report(
            GuidGenerator.Create(),
            reportNumber,
            createInput.ReportDate,
            createInput.TestRequestId
        );

        entity.RecipientEmail = createInput.RecipientEmail;
        entity.RecipientName = createInput.RecipientName;
        entity.Standards = createInput.Standards;
        entity.Conclusion = createInput.Conclusion;
        entity.Notes = createInput.Notes;

        return entity;
    }

    protected override async Task MapToEntityAsync(UpdateReportDto updateInput, Report entity)
    {
        entity.ReportDate = updateInput.ReportDate;
        entity.RecipientEmail = updateInput.RecipientEmail;
        entity.RecipientName = updateInput.RecipientName;
        entity.Standards = updateInput.Standards;
        entity.Conclusion = updateInput.Conclusion;
        entity.Notes = updateInput.Notes;

        await Task.CompletedTask;
    }

    public override async Task<ReportDto> CreateAsync(CreateReportDto input)
    {
        var report = await base.CreateAsync(input);

        // Add test results to the report
        if (input.TestResultIds != null && input.TestResultIds.Any())
        {
            foreach (var testResultId in input.TestResultIds)
            {
                await _reportTestResultRepository.InsertAsync(
                    new ReportTestResult(report.Id, testResultId)
                );
            }
        }

        return report;
    }

    public override async Task<ReportDto> UpdateAsync(Guid id, UpdateReportDto input)
    {
        var report = await base.UpdateAsync(id, input);

        // Update test results
        // First, remove all existing relationships
        var queryable = await _reportTestResultRepository.GetQueryableAsync();
        var existingReportTestResults = await AsyncExecuter.ToListAsync(
            queryable.Where(r => r.ReportId == id)
        );

        await _reportTestResultRepository.DeleteManyAsync(existingReportTestResults);

        // Then, add new relationships
        if (input.TestResultIds != null && input.TestResultIds.Any())
        {
            foreach (var testResultId in input.TestResultIds)
            {
                await _reportTestResultRepository.InsertAsync(
                    new ReportTestResult(id, testResultId)
                );
            }
        }

        return report;
    }

    protected override async Task<IQueryable<Report>> CreateFilteredQueryAsync(PagedAndSortedResultRequestDto input)
    {
        return (await base.CreateFilteredQueryAsync(input))
            .Include(r => r.ReportTestResults);
    }

    public override async Task<ReportDto> GetAsync(Guid id)
    {
        var report = await Repository.GetAsync(id);
        var dto = await MapToGetOutputDtoAsync(report);

        // Load test result IDs
        var queryable = await _reportTestResultRepository.GetQueryableAsync();
        var testResultIds = await AsyncExecuter.ToListAsync(
            queryable.Where(r => r.ReportId == id).Select(r => r.TestResultId)
        );

        dto.TestResultIds = testResultIds;

        return dto;
    }

    /// <summary>
    /// Generate QR code for the report
    /// </summary>
    public async Task<ReportDto> GenerateQRCodeAsync(Guid id)
    {
        var report = await Repository.GetAsync(id);

        // Generate QR code content (report URL or ID)
        var qrContent = $"REPORT-{report.ReportNumber}";

        using (var qrGenerator = new QRCodeGenerator())
        {
            var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            using (var qrCode = new PngByteQRCode(qrCodeData))
            {
                var qrCodeImage = qrCode.GetGraphic(20);
                // Convert to base64 string
                report.QRCode = Convert.ToBase64String(qrCodeImage);
            }
        }

        await Repository.UpdateAsync(report);

        return await MapToGetOutputDtoAsync(report);
    }

    /// <summary>
    /// Approve the report
    /// </summary>
    public async Task<ReportDto> ApproveAsync(Guid id)
    {
        var report = await Repository.GetAsync(id);

        if (report.Status != ReportStatus.Draft)
        {
            throw new BusinessException("Only draft reports can be approved");
        }

        report.Status = ReportStatus.Approved;
        report.ApprovedById = CurrentUser.Id;
        report.ApprovedDate = DateTime.Now;

        await Repository.UpdateAsync(report);

        return await MapToGetOutputDtoAsync(report);
    }

    /// <summary>
    /// Send report via email
    /// </summary>
    public async Task<ReportDto> SendReportAsync(Guid id)
    {
        var report = await Repository.GetAsync(id);

        if (report.Status != ReportStatus.Approved)
        {
            throw new BusinessException("Only approved reports can be sent");
        }

        // TODO: Implement email sending logic here
        // For now, just update the status

        report.Status = ReportStatus.Sent;
        report.SentDate = DateTime.Now;

        await Repository.UpdateAsync(report);

        return await MapToGetOutputDtoAsync(report);
    }

    /// <summary>
    /// Cancel the report
    /// </summary>
    public async Task<ReportDto> CancelAsync(Guid id)
    {
        var report = await Repository.GetAsync(id);

        if (report.Status == ReportStatus.Sent)
        {
            throw new BusinessException("Sent reports cannot be cancelled");
        }

        report.Status = ReportStatus.Cancelled;

        await Repository.UpdateAsync(report);

        return await MapToGetOutputDtoAsync(report);
    }

    /// <summary>
    /// Export report as PDF
    /// </summary>
    public async Task<byte[]> ExportToPdfAsync(Guid id)
    {
        var report = await Repository.GetAsync(id);

        // TODO: Implement PDF generation logic
        // For now, return empty byte array
        await Task.CompletedTask;

        throw new NotImplementedException("PDF export will be implemented in the next phase");
    }

    /// <summary>
    /// Get reports by Test Request ID
    /// </summary>
    public async Task<PagedResultDto<ReportDto>> GetByTestRequestIdAsync(
        Guid testRequestId,
        PagedAndSortedResultRequestDto input)
    {
        var query = (await Repository.GetQueryableAsync())
            .Where(r => r.TestRequestId == testRequestId);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var reports = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(r => r.ReportDate)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
        );

        var dtos = ObjectMapper.Map<List<Report>, List<ReportDto>>(reports);

        return new PagedResultDto<ReportDto>(totalCount, dtos);
    }

    private async Task<string> GenerateReportNumberAsync()
    {
        // Generate report number format: REP-YYYYMMDD-XXXX
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"REP-{today}-";

        var queryable = await Repository.GetQueryableAsync();
        var lastReport = await AsyncExecuter.FirstOrDefaultAsync(
            queryable
                .Where(r => r.ReportNumber.StartsWith(prefix))
                .OrderByDescending(r => r.ReportNumber)
        );

        if (lastReport == null)
        {
            return $"{prefix}0001";
        }

        var lastNumber = int.Parse(lastReport.ReportNumber.Substring(prefix.Length));
        var newNumber = lastNumber + 1;

        return $"{prefix}{newNumber:D4}";
    }
}
