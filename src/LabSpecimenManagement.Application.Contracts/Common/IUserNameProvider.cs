using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Service for retrieving user display names
/// Centralizes user name lookup logic to avoid duplication
/// </summary>
public interface IUserNameProvider
{
    /// <summary>
    /// Gets display names for multiple users by their IDs
    /// Returns a dictionary mapping user IDs to their display names
    /// </summary>
    /// <param name="userIds">Collection of nullable user IDs</param>
    /// <returns>Dictionary with user ID as key and display name as value</returns>
    Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid?> userIds);

    /// <summary>
    /// Gets display name for a single user by ID
    /// </summary>
    /// <param name="userId">Nullable user ID</param>
    /// <returns>User display name or null if not found</returns>
    Task<string?> GetUserNameAsync(Guid? userId);

    /// <summary>
    /// Enriches DTOs with user names based on their user ID properties
    /// </summary>
    /// <typeparam name="TDto">DTO type</typeparam>
    /// <param name="dtos">Collection of DTOs to enrich</param>
    /// <param name="userIdSelector">Function to get user ID from DTO</param>
    /// <param name="userNameSetter">Action to set user name on DTO</param>
    Task EnrichWithUserNamesAsync<TDto>(
        IEnumerable<TDto> dtos,
        Func<TDto, Guid?> userIdSelector,
        Action<TDto, string> userNameSetter);
}
