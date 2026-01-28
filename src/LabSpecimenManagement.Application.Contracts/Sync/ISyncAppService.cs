using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Service for handling offline data synchronization
/// Enables offline-first functionality for all entity types
/// </summary>
public interface ISyncAppService : IApplicationService
{
    /// <summary>
    /// Process a batch of test requests from offline queue (legacy endpoint)
    /// Handles creates, updates, and conflict resolution using "last write wins" strategy
    /// </summary>
    /// <param name="input">Batch of test requests to sync</param>
    /// <returns>Sync results with success/failure status and conflict resolution info</returns>
    Task<SyncBatchResponseDto> SyncTestRequestsAsync(SyncBatchRequestDto input);

    /// <summary>
    /// Get server changes since last sync timestamp (legacy endpoint for test requests)
    /// Used for bidirectional sync to pull server changes to client
    /// </summary>
    /// <param name="input">Query parameters including last sync timestamp</param>
    /// <returns>List of changed test requests and deleted IDs</returns>
    Task<ServerChangesResponseDto> GetServerChangesAsync(GetServerChangesDto input);

    /// <summary>
    /// Get current server timestamp
    /// Used by client to initialize sync state
    /// </summary>
    /// <returns>Current server UTC timestamp</returns>
    Task<SyncTimestampDto> GetServerTimestampAsync();

    // ============= Generic Sync Methods =============

    /// <summary>
    /// Process a batch of items from offline queue (supports all entity types)
    /// Handles creates, updates, and conflict resolution using "last write wins" strategy
    /// </summary>
    /// <param name="input">Batch of items to sync (can be mixed entity types)</param>
    /// <returns>Sync results with success/failure status and conflict resolution info</returns>
    Task<GenericSyncBatchResponseDto> SyncBatchAsync(GenericSyncBatchRequestDto input);

    /// <summary>
    /// Get server changes for a specific entity type since last sync timestamp
    /// </summary>
    /// <param name="input">Query parameters including entity type and last sync timestamp</param>
    /// <returns>List of changed entities and deleted IDs</returns>
    Task<GenericServerChangesResponseDto> GetEntityChangesAsync(GetEntityChangesDto input);
}

/// <summary>
/// DTO for server timestamp response
/// </summary>
public class SyncTimestampDto
{
    /// <summary>
    /// Current server UTC timestamp
    /// </summary>
    public System.DateTime ServerTimestamp { get; set; }
}
