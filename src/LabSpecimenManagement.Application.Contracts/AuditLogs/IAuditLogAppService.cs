using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.AuditLogs;

public interface IAuditLogAppService : IApplicationService
{
    Task<PagedResultDto<AuditLogDto>> GetListAsync(GetAuditLogsInput input);
    Task<AuditLogDto> GetAsync(Guid id);
    Task<ListResultDto<EntityChangeDto>> GetEntityChangesAsync(Guid auditLogId);
    Task<AuditLogStatisticsDto> GetStatisticsAsync();
}
