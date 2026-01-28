using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Supported entity types for offline sync
/// </summary>
public enum SyncEntityType
{
    TestRequest,
    Quotation,
    Receipt,
    NCR,
    Specimen,
    QualityManagement,
    Testing,
    Subcontractor,
    Employee,
    LaboratoryTask,
    StandardIndicator,
    TestMethod,
    Laboratory,
    SpecimenType,
    TestResult
}

/// <summary>
/// Generic DTO for a single sync item that works with any entity type
/// </summary>
public class GenericSyncItemDto
{
    /// <summary>
    /// Temporary client-side ID (UUID generated on client)
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
    /// Entity type being synced
    /// </summary>
    public SyncEntityType EntityType { get; set; }

    /// <summary>
    /// Client's last modification timestamp for conflict detection
    /// </summary>
    [Required]
    public DateTime ClientLastModified { get; set; }

    /// <summary>
    /// The entity data to sync (JSON object, will be deserialized based on EntityType)
    /// </summary>
    [Required]
    public object Data { get; set; } = new();
}

/// <summary>
/// Generic DTO for batch sync request supporting multiple entity types
/// </summary>
public class GenericSyncBatchRequestDto
{
    /// <summary>
    /// List of items to sync (can be mixed entity types)
    /// </summary>
    [Required]
    public List<GenericSyncItemDto> Items { get; set; } = new();

    /// <summary>
    /// Unique identifier for the device/browser
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
    /// </summary>
    public DateTime? LastSyncTimestamp { get; set; }
}

/// <summary>
/// Generic DTO for individual sync result
/// </summary>
public class GenericSyncResultDto
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
    /// Entity type that was synced
    /// </summary>
    public SyncEntityType EntityType { get; set; }

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
    /// The synced/current server data
    /// </summary>
    public object? ServerData { get; set; }
}

/// <summary>
/// Generic DTO for batch sync response
/// </summary>
public class GenericSyncBatchResponseDto
{
    /// <summary>
    /// Individual results for each synced item
    /// </summary>
    public List<GenericSyncResultDto> Results { get; set; } = new();

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
    /// </summary>
    public DateTime ServerTimestamp { get; set; }

    /// <summary>
    /// Summary of synced items by entity type
    /// </summary>
    public Dictionary<SyncEntityType, int> SyncedByType { get; set; } = new();
}

/// <summary>
/// DTO for requesting server changes for a specific entity type
/// </summary>
public class GetEntityChangesDto
{
    /// <summary>
    /// Entity type to get changes for
    /// </summary>
    public SyncEntityType EntityType { get; set; }

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
/// Generic DTO for server changes response
/// </summary>
public class GenericServerChangesResponseDto
{
    /// <summary>
    /// Entity type for these changes
    /// </summary>
    public SyncEntityType EntityType { get; set; }

    /// <summary>
    /// Entities that have been modified since last sync
    /// </summary>
    public List<object> Changes { get; set; } = new();

    /// <summary>
    /// IDs of entities that have been deleted since last sync
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
