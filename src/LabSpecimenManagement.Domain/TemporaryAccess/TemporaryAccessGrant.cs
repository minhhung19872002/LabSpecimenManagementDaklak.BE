using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TemporaryAccess;

public class TemporaryAccessGrant : FullAuditedAggregateRoot<Guid>
{
    public TemporaryEmployeeType EmployeeType { get; set; }

    // Internal employee (UserId of existing IdentityUser)
    public Guid? UserId { get; set; }

    // External contractor (UserId of newly created IdentityUser)
    public Guid? ExternalUserId { get; set; }
    public string? ExternalUsername { get; set; }

    // Denormalized display fields — set at creation time, not updated
    public string EmployeeFullName { get; set; } = string.Empty;
    public string? EmployeeEmail { get; set; }
    public string? EmployeePhone { get; set; }

    // Time window: StartDate = today 00:00, ExpiryDate = chosen day 23:59:59
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    // JSON string[] of group keys selected in modal (e.g. ["PhieuYeuCau","BaoGia"])
    public string SelectedModules { get; set; } = "[]";

    // JSON string[] of permission names actually inserted into AbpPermissionGrants
    // Only permissions the user did NOT already have as direct grant are stored here.
    // Revocation ONLY deletes these exact rows — permanent grants are never touched.
    public string GrantedPermissions { get; set; } = "[]";

    public GrantStatus Status { get; set; } = GrantStatus.Active;
    public string? Reason { get; set; }
}
