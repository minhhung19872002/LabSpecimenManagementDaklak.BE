using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.AuditLogging;

namespace LabSpecimenManagement.AuditLogs;

[RemoteService(IsEnabled = false)]
[Authorize(Permissions.LabSpecimenManagementPermissions.AuditLogs.Default)]
public class AuditLogAppService : ApplicationService, IAuditLogAppService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogAppService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResultDto<AuditLogDto>> GetListAsync(GetAuditLogsInput input)
    {
        HttpStatusCode? httpStatusCode = input.HttpStatusCode.HasValue
            ? (HttpStatusCode)input.HttpStatusCode.Value
            : null;

        var count = await _auditLogRepository.GetCountAsync(
            startTime: input.StartTime,
            endTime: input.EndTime,
            httpMethod: input.HttpMethod,
            url: input.Url,
            userId: input.UserId,
            userName: input.UserName,
            applicationName: input.ApplicationName,
            clientIpAddress: input.ClientIpAddress,
            correlationId: input.CorrelationId,
            maxExecutionDuration: input.MaxExecutionDuration,
            minExecutionDuration: input.MinExecutionDuration,
            hasException: input.HasException,
            httpStatusCode: httpStatusCode
        );

        var items = await _auditLogRepository.GetListAsync(
            sorting: input.Sorting ?? "ExecutionTime DESC",
            maxResultCount: input.MaxResultCount,
            skipCount: input.SkipCount,
            startTime: input.StartTime,
            endTime: input.EndTime,
            httpMethod: input.HttpMethod,
            url: input.Url,
            userId: input.UserId,
            userName: input.UserName,
            applicationName: input.ApplicationName,
            clientIpAddress: input.ClientIpAddress,
            correlationId: input.CorrelationId,
            maxExecutionDuration: input.MaxExecutionDuration,
            minExecutionDuration: input.MinExecutionDuration,
            hasException: input.HasException,
            httpStatusCode: httpStatusCode,
            includeDetails: false
        );

        return new PagedResultDto<AuditLogDto>(
            count,
            items.Select(x => MapToDto(x)).ToList()
        );
    }

    public async Task<AuditLogDto> GetAsync(Guid id)
    {
        var auditLog = await _auditLogRepository.GetAsync(id);
        return MapToDto(auditLog, includeDetails: true);
    }

    public async Task<ListResultDto<EntityChangeDto>> GetEntityChangesAsync(Guid auditLogId)
    {
        var auditLog = await _auditLogRepository.GetAsync(auditLogId);
        var entityChanges = auditLog.EntityChanges.Select(MapToEntityChangeDto).ToList();
        return new ListResultDto<EntityChangeDto>(entityChanges);
    }

    public async Task<AuditLogStatisticsDto> GetStatisticsAsync()
    {
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);

        var total = await _auditLogRepository.GetCountAsync();
        var todayCount = await _auditLogRepository.GetCountAsync(startTime: today, endTime: tomorrow);
        var gets = await _auditLogRepository.GetCountAsync(httpMethod: "GET");
        var posts = await _auditLogRepository.GetCountAsync(httpMethod: "POST");
        var puts = await _auditLogRepository.GetCountAsync(httpMethod: "PUT");
        var deletes = await _auditLogRepository.GetCountAsync(httpMethod: "DELETE");

        return new AuditLogStatisticsDto
        {
            Total = total,
            Today = todayCount,
            Gets = gets,
            Posts = posts,
            Puts = puts,
            Deletes = deletes
        };
    }

    private AuditLogDto MapToDto(AuditLog auditLog, bool includeDetails = false)
    {
        var dto = new AuditLogDto
        {
            Id = auditLog.Id,
            ApplicationName = auditLog.ApplicationName,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            TenantId = auditLog.TenantId,
            TenantName = auditLog.TenantName,
            ImpersonatorUserId = auditLog.ImpersonatorUserId,
            ImpersonatorTenantId = auditLog.ImpersonatorTenantId,
            ExecutionTime = auditLog.ExecutionTime,
            ExecutionDuration = auditLog.ExecutionDuration,
            ClientIpAddress = auditLog.ClientIpAddress,
            ClientName = auditLog.ClientName,
            ClientId = auditLog.ClientId,
            CorrelationId = auditLog.CorrelationId,
            BrowserInfo = auditLog.BrowserInfo,
            HttpMethod = auditLog.HttpMethod,
            Url = auditLog.Url,
            HttpStatusCode = auditLog.HttpStatusCode,
            Comments = auditLog.Comments
        };

        if (includeDetails)
        {
            dto.Exceptions = auditLog.Exceptions;

            dto.Actions = auditLog.Actions.Select(a => new AuditLogActionDto
            {
                Id = a.Id,
                ServiceName = a.ServiceName,
                MethodName = a.MethodName,
                Parameters = a.Parameters,
                ExecutionTime = a.ExecutionTime,
                ExecutionDuration = a.ExecutionDuration
            }).ToList();

            dto.EntityChanges = auditLog.EntityChanges.Select(MapToEntityChangeDto).ToList();
        }

        return dto;
    }

    private EntityChangeDto MapToEntityChangeDto(EntityChange entityChange)
    {
        return new EntityChangeDto
        {
            Id = entityChange.Id,
            AuditLogId = entityChange.AuditLogId,
            ChangeTime = entityChange.ChangeTime,
            ChangeType = entityChange.ChangeType.ToString(),
            EntityTenantId = entityChange.EntityTenantId,
            EntityId = entityChange.EntityId,
            EntityTypeFullName = entityChange.EntityTypeFullName,
            PropertyChanges = entityChange.PropertyChanges.Select(p => new EntityPropertyChangeDto
            {
                Id = p.Id,
                EntityChangeId = p.EntityChangeId,
                NewValue = p.NewValue,
                OriginalValue = p.OriginalValue,
                PropertyName = p.PropertyName,
                PropertyTypeFullName = p.PropertyTypeFullName
            }).ToList()
        };
    }
}
