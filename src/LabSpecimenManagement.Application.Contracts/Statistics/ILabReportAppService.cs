using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace LabSpecimenManagement.Statistics;

public interface ILabReportAppService : IApplicationService
{
    Task<LabReportDataDto> GetLabReportDataAsync(Guid labId, DateTime fromDate, DateTime toDate);
    Task<LabContractDetailDto> GetLabContractDetailAsync(Guid testRequestId, Guid labId);
    Task<IRemoteStreamContent> ExportLabReportAsync(Guid labId, DateTime fromDate, DateTime toDate);
    Task<IRemoteStreamContent> ExportLabReportDetailAsync(Guid labId, DateTime fromDate, DateTime toDate);
}
