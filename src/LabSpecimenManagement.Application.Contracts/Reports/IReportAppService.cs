using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace LabSpecimenManagement.Reports;

public interface IReportAppService : ICrudAppService<
    ReportDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateReportDto,
    UpdateReportDto>
{
    Task<ReportDto> SubmitForApprovalAsync(Guid id);
    Task<ReportDto> ApproveAsync(Guid id);
    Task<ReportDto> RejectAsync(Guid id, string? reason);
    Task<ReportDto> IssueAsync(Guid id);
    Task SendEmailAsync(Guid id);
    Task<ReportDto?> GetByReportNumberAsync(string reportNumber);
    Task<ListResultDto<ReportDto>> GetByTestRequestAsync(Guid testRequestId);
    Task<IRemoteStreamContent> ExportPdfAsync(Guid id);
    Task<ReportVerifyResultDto> VerifyReportAsync(string reportNumber);
}
