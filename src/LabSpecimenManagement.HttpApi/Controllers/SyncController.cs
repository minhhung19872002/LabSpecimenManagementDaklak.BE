using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// Controller for offline data synchronization
/// Enables PWA/offline-first functionality
/// </summary>
[RemoteService(Name = "Default")]
[Route("api/v1/sync")]
[Authorize(LabSpecimenManagementPermissions.Sync.Default)]
public class SyncController : LabSpecimenManagementController
{
    private readonly ISyncAppService _syncAppService;

    public SyncController(ISyncAppService syncAppService)
    {
        _syncAppService = syncAppService;
    }

    /// <summary>
    /// Sync a batch of test requests from offline queue
    /// Handles creates, updates, deletes with "last write wins" conflict resolution
    /// </summary>
    /// <param name="input">Batch of test requests to sync</param>
    /// <returns>Sync results with success/failure status and server changes</returns>
    [HttpPost("test-requests")]
    [Authorize(LabSpecimenManagementPermissions.Sync.TestRequests)]
    public virtual Task<SyncBatchResponseDto> SyncTestRequestsAsync([FromBody] SyncBatchRequestDto input)
    {
        return _syncAppService.SyncTestRequestsAsync(input);
    }

    /// <summary>
    /// Get server changes since last sync timestamp
    /// Used for bidirectional sync to pull server changes to client
    /// </summary>
    /// <param name="input">Query parameters including last sync timestamp</param>
    /// <returns>List of changed test requests and deleted IDs</returns>
    [HttpPost("test-requests/changes")]
    public virtual Task<ServerChangesResponseDto> GetServerChangesAsync([FromBody] GetServerChangesDto input)
    {
        return _syncAppService.GetServerChangesAsync(input);
    }

    /// <summary>
    /// Get current server timestamp
    /// Used by client to initialize sync state
    /// </summary>
    /// <returns>Current server UTC timestamp</returns>
    [HttpGet("timestamp")]
    public virtual Task<SyncTimestampDto> GetServerTimestampAsync()
    {
        return _syncAppService.GetServerTimestampAsync();
    }

    // ============= Generic Sync Endpoints =============

    /// <summary>
    /// Sync a batch of items from offline queue (supports all entity types)
    /// Handles creates, updates, deletes with "last write wins" conflict resolution
    /// </summary>
    /// <param name="input">Batch of items to sync (can be mixed entity types)</param>
    /// <returns>Sync results with success/failure status</returns>
    [HttpPost("batch")]
    public virtual Task<GenericSyncBatchResponseDto> SyncBatchAsync([FromBody] GenericSyncBatchRequestDto input)
    {
        return _syncAppService.SyncBatchAsync(input);
    }

    /// <summary>
    /// Get server changes for a specific entity type since last sync
    /// </summary>
    /// <param name="input">Query parameters including entity type and last sync timestamp</param>
    /// <returns>List of changed entities and deleted IDs</returns>
    [HttpPost("changes")]
    public virtual Task<GenericServerChangesResponseDto> GetEntityChangesAsync([FromBody] GetEntityChangesDto input)
    {
        return _syncAppService.GetEntityChangesAsync(input);
    }

}