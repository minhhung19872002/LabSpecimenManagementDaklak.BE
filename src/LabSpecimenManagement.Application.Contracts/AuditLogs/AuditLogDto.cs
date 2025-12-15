using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.AuditLogs;

public class AuditLogDto : EntityDto<Guid>
{
    public string? ApplicationName { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? ImpersonatorUserId { get; set; }
    public Guid? ImpersonatorTenantId { get; set; }
    public DateTime ExecutionTime { get; set; }
    public int ExecutionDuration { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? ClientName { get; set; }
    public string? ClientId { get; set; }
    public string? CorrelationId { get; set; }
    public string? BrowserInfo { get; set; }
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? Comments { get; set; }
    public List<AuditLogActionDto> Actions { get; set; } = new();
    public List<EntityChangeDto> EntityChanges { get; set; } = new();
}

public class AuditLogActionDto
{
    public Guid Id { get; set; }
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
    public string? Parameters { get; set; }
    public DateTime ExecutionTime { get; set; }
    public int ExecutionDuration { get; set; }
}

public class EntityChangeDto
{
    public Guid Id { get; set; }
    public Guid AuditLogId { get; set; }
    public DateTime ChangeTime { get; set; }
    public string? ChangeType { get; set; }
    public Guid? EntityTenantId { get; set; }
    public string? EntityId { get; set; }
    public string? EntityTypeFullName { get; set; }
    public List<EntityPropertyChangeDto> PropertyChanges { get; set; } = new();
}

public class EntityPropertyChangeDto
{
    public Guid Id { get; set; }
    public Guid EntityChangeId { get; set; }
    public string? NewValue { get; set; }
    public string? OriginalValue { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertyTypeFullName { get; set; }
}
