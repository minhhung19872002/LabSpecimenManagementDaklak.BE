using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Enum for sync conflict resolution strategy
/// </summary>
public enum SyncConflictResolution
{
    /// <summary>
    /// Client's changes were applied (client timestamp was newer)
    /// </summary>
    ClientWins,

    /// <summary>
    /// Server's data was kept (server timestamp was newer)
    /// </summary>
    ServerWins,

    /// <summary>
    /// Data was merged from both sources
    /// </summary>
    Merged
}

/// <summary>
/// Enum for sync operation type
/// </summary>
public enum SyncOperationType
{
    Create,
    Update,
    Delete
}

/// <summary>
/// DTO for a single test request sync item
/// </summary>
public class SyncTestRequestDto
{
    /// <summary>
    /// Temporary client-side ID (UUID generated on client)
    /// Used to track the item through the sync process
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Server-side ID (null for new items, Guid for updates)
    /// </summary>
    public Guid? ServerId { get; set; }

    /// <summary>
    /// Type of sync operation
    /// </summary>
    public SyncOperationType OperationType { get; set; } = SyncOperationType.Create;

    /// <summary>
    /// Client's last modification timestamp for conflict detection
    /// </summary>
    [Required]
    public DateTime ClientLastModified { get; set; }

    /// <summary>
    /// The test request data to sync
    /// </summary>
    [Required]
    public CreateTestRequestDto Data { get; set; } = new();
}

/// <summary>
/// DTO for batch sync request
/// </summary>
public class SyncBatchRequestDto
{
    /// <summary>
    /// List of test requests to sync
    /// </summary>
    [Required]
    public List<SyncTestRequestDto> TestRequests { get; set; } = new();

    /// <summary>
    /// Unique identifier for the device/browser
    /// Used for tracking sync sessions
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when sync was initiated on client
    /// </summary>
    [Required]
    public DateTime SyncTimestamp { get; set; }

    /// <summary>
    /// Last successful sync timestamp from client
    /// Used to fetch server changes since this time
    /// </summary>
    public DateTime? LastSyncTimestamp { get; set; }
}

/// <summary>
/// DTO for individual sync result
/// </summary>
public class SyncResultDto
{
    /// <summary>
    /// Whether the sync operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The client-side ID that was synced
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The server-side ID (assigned for new items or existing for updates)
    /// </summary>
    public Guid? ServerId { get; set; }

    /// <summary>
    /// Error message if sync failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Validation errors if any
    /// </summary>
    public List<string>? ValidationErrors { get; set; }

    /// <summary>
    /// How the conflict was resolved (if applicable)
    /// </summary>
    public SyncConflictResolution? ConflictResolution { get; set; }

    /// <summary>
    /// The synced/current server data (returned for conflict resolution info)
    /// </summary>
    public TestRequestDto? ServerData { get; set; }
}

/// <summary>
/// DTO for batch sync response
/// </summary>
public class SyncBatchResponseDto
{
    /// <summary>
    /// Individual results for each synced item
    /// </summary>
    public List<SyncResultDto> Results { get; set; } = new();

    /// <summary>
    /// Number of successfully synced items
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed items
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Number of items with conflicts
    /// </summary>
    public int ConflictCount { get; set; }

    /// <summary>
    /// Server timestamp at time of sync completion
    /// Client should store this for next sync
    /// </summary>
    public DateTime ServerTimestamp { get; set; }

    /// <summary>
    /// Server changes since client's last sync (for bidirectional sync)
    /// </summary>
    public List<TestRequestDto>? ServerChanges { get; set; }
}

/// <summary>
/// DTO for requesting server changes since last sync
/// </summary>
public class GetServerChangesDto
{
    /// <summary>
    /// Last successful sync timestamp
    /// </summary>
    [Required]
    public DateTime LastSyncTimestamp { get; set; }

    /// <summary>
    /// Device ID for filtering (optional)
    /// </summary>
    [StringLength(100)]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Maximum number of changes to return
    /// </summary>
    public int MaxResults { get; set; } = 100;
}

/// <summary>
/// DTO for server changes response
/// </summary>
public class ServerChangesResponseDto
{
    /// <summary>
    /// Test requests that have been modified since last sync
    /// </summary>
    public List<TestRequestDto> Changes { get; set; } = new();

    /// <summary>
    /// IDs of test requests that have been deleted since last sync
    /// </summary>
    public List<Guid> DeletedIds { get; set; } = new();

    /// <summary>
    /// Whether there are more changes available
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Server timestamp to use for next sync
    /// </summary>
    public DateTime ServerTimestamp { get; set; }

    /// <summary>
    /// Total count of changes available
    /// </summary>
    public int TotalCount { get; set; }
}
