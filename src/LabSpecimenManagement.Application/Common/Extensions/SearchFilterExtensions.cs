using System;
using System.Linq;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Extension methods for search and filter operations
/// Note: WhereIf is already provided by ABP's AbpQueryableExtensions
/// </summary>
public static class SearchFilterExtensions
{
    /// <summary>
    /// Case-insensitive string contains check
    /// </summary>
    /// <param name="source">Source string to search in</param>
    /// <param name="search">Search string to find</param>
    /// <returns>True if source contains search string (case-insensitive)</returns>
    public static bool ContainsIgnoreCase(this string? source, string? search)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(search))
        {
            return false;
        }

        return source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if any of the provided string values contain the search term (case-insensitive)
    /// Useful for searching across multiple fields
    /// </summary>
    /// <param name="search">Search term</param>
    /// <param name="values">String values to search in</param>
    /// <returns>True if any value contains the search term</returns>
    public static bool MatchesAny(string? search, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return values.Any(v => v.ContainsIgnoreCase(search));
    }

    /// <summary>
    /// Normalizes a search string by trimming and converting to lowercase
    /// </summary>
    /// <param name="search">Search string to normalize</param>
    /// <returns>Normalized search string or null if empty</returns>
    public static string? NormalizeSearch(this string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        return search.Trim().ToLowerInvariant();
    }
}
