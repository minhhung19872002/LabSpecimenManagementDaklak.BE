using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Registry for sync entity handlers
/// Provides access to the appropriate handler based on entity type
/// </summary>
public class SyncHandlerRegistry : ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;

    public SyncHandlerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Get the sync handler for a specific entity type
    /// </summary>
    /// <param name="entityType">The entity type to get handler for</param>
    /// <returns>The sync handler or null if not found</returns>
    public ISyncEntityHandler? GetHandler(SyncEntityType entityType)
    {
        var handlers = _serviceProvider.GetServices<ISyncEntityHandler>();
        return handlers.FirstOrDefault(h => h.EntityType == entityType);
    }

    /// <summary>
    /// Get all registered sync handlers
    /// </summary>
    public IEnumerable<ISyncEntityHandler> GetAllHandlers()
    {
        return _serviceProvider.GetServices<ISyncEntityHandler>();
    }

    /// <summary>
    /// Check if a handler exists for an entity type
    /// </summary>
    public bool HasHandler(SyncEntityType entityType)
    {
        return GetHandler(entityType) != null;
    }

    /// <summary>
    /// Get all supported entity types
    /// </summary>
    public IEnumerable<SyncEntityType> GetSupportedEntityTypes()
    {
        return GetAllHandlers().Select(h => h.EntityType).Distinct();
    }
}
