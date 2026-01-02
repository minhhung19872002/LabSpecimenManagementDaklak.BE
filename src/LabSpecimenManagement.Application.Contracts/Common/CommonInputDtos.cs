using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Common input DTO for toggling active status
/// </summary>
public class ToggleActiveDto
{
    /// <summary>
    /// New active status
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Common input DTO for refresh token requests
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// The refresh token to use
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Common input DTO for rejection with reason
/// </summary>
public class RejectWithReasonDto
{
    /// <summary>
    /// Reason for rejection
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Common input DTO for bulk operations with IDs
/// </summary>
public class BulkIdsDto
{
    /// <summary>
    /// List of IDs to operate on
    /// </summary>
    [Required]
    public List<Guid> Ids { get; set; } = new();
}

/// <summary>
/// Common input DTO for bulk approval operations
/// </summary>
public class BulkApprovalDto
{
    /// <summary>
    /// List of IDs to approve
    /// </summary>
    [Required]
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Whether to approve (true) or reject (false)
    /// </summary>
    public bool Approve { get; set; } = true;

    /// <summary>
    /// Reason for rejection (required when Approve = false)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Optional mapping of indicator IDs to standard IDs
    /// </summary>
    public Dictionary<Guid, Guid?>? StandardIds { get; set; }
}
