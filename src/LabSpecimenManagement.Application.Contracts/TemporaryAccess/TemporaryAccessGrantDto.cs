using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TemporaryAccess;

public class TemporaryAccessGrantDto : EntityDto<Guid>
{
    public TemporaryEmployeeType EmployeeType { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ExternalUserId { get; set; }
    public string? ExternalUsername { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string? EmployeeEmail { get; set; }
    public string? EmployeePhone { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public GrantStatus Status { get; set; }
    public List<string> SelectedModules { get; set; } = new();
    public string? Reason { get; set; }
    public DateTime CreationTime { get; set; }
}
