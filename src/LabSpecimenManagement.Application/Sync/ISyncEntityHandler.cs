using System;
using System.Threading.Tasks;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Interface for entity-specific sync handlers
/// Each entity type that supports offline sync must have a handler implementation
/// </summary>
public interface ISyncEntityHandler
{
    /// <summary>
    /// The entity type this handler supports
    /// </summary>
    SyncEntityType EntityType { get; }

    /// <summary>
    /// true: bản ghi tạo từ sync lấy luôn ClientId làm Id (xem <see cref="SyncCreateContext"/>) → chống tạo trùng bền vững
    /// </summary>
    bool UsesClientIdAsServerId => false;

    /// <summary>
    /// Process a create operation
    /// </summary>
    /// <param name="data">The entity data (JSON object)</param>
    /// <returns>Result with server ID and data</returns>
    Task<(Guid ServerId, object ServerData)> CreateAsync(object data);

    /// <summary>
    /// Process an update operation
    /// </summary>
    /// <param name="serverId">The server-side entity ID</param>
    /// <param name="data">The entity data (JSON object)</param>
    /// <returns>Updated entity data</returns>
    Task<object> UpdateAsync(Guid serverId, object data);

    /// <summary>
    /// Process a delete operation
    /// </summary>
    /// <param name="serverId">The server-side entity ID</param>
    Task DeleteAsync(Guid serverId);

    /// <summary>
    /// Get an entity by ID
    /// </summary>
    /// <param name="serverId">The server-side entity ID</param>
    /// <returns>The entity data</returns>
    Task<object?> GetAsync(Guid serverId);

    /// <summary>
    /// Get the last modification time for an entity
    /// </summary>
    /// <param name="serverId">The server-side entity ID</param>
    /// <returns>Last modification time or null if not found</returns>
    Task<DateTime?> GetLastModificationTimeAsync(Guid serverId);

    /// <summary>
    /// Check if an entity exists
    /// </summary>
    /// <param name="serverId">The server-side entity ID</param>
    Task<bool> ExistsAsync(Guid serverId);
}
