using LabSpecimenManagement.AuditLogs;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/audit-logs")]
[Authorize(LabSpecimenManagementPermissions.AuditLogs.Default)]
public class AuditLogController : LabSpecimenManagementController
{
    private readonly IAuditLogAppService _auditLogAppService;

    public AuditLogController(IAuditLogAppService auditLogAppService)
    {
        _auditLogAppService = auditLogAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<AuditLogDto>> GetListAsync([FromQuery] GetAuditLogsInput input)
    {
        return _auditLogAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<AuditLogDto> GetAsync(Guid id)
    {
        return _auditLogAppService.GetAsync(id);
    }

    [HttpGet("{id}/entity-changes")]
    public virtual Task<ListResultDto<EntityChangeDto>> GetEntityChangesAsync(Guid id)
    {
        return _auditLogAppService.GetEntityChangesAsync(id);
    }

    [HttpGet("statistics")]
    public virtual Task<AuditLogStatisticsDto> GetStatisticsAsync()
    {
        return _auditLogAppService.GetStatisticsAsync();
    }
}
