using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Extension methods for IEnumerable to provide common operations
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Filters items that contain the search text (case-insensitive) in any of the specified fields
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    /// <param name="source">Source collection</param>
    /// <param name="searchText">Text to search for</param>
    /// <param name="fieldSelectors">Functions to get string fields from item</param>
    /// <returns>Filtered collection</returns>
    public static IEnumerable<T> WhereContainsIgnoreCase<T>(
        this IEnumerable<T> source,
        string? searchText,
        params Func<T, string?>[] fieldSelectors)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return source;

        var search = searchText.Trim();
        return source.Where(item =>
            fieldSelectors.Any(selector =>
                selector(item).ContainsIgnoreCase(search)));
    }
}

/// <summary>
/// Extension methods for IQueryable to provide common query operations
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Filters the query to include only active entities
    /// </summary>
    /// <typeparam name="T">Entity type that implements IHasActive</typeparam>
    /// <param name="query">Source query</param>
    /// <returns>Filtered query with only active entities</returns>
    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query) where T : IHasActive
    {
        return query.Where(x => x.IsActive);
    }

    /// <summary>
    /// Applies paging to the query using skip and take
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">Source query</param>
    /// <param name="input">Paged request containing SkipCount and MaxResultCount</param>
    /// <returns>Paged query</returns>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, IPagedResultRequest input)
    {
        return query.Skip(input.SkipCount).Take(input.MaxResultCount);
    }

    /// <summary>
    /// Applies sorting to the query with a default sort expression as fallback
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TKey">Sort key type</typeparam>
    /// <param name="query">Source query</param>
    /// <param name="sorting">Dynamic sorting string (e.g., "Name desc")</param>
    /// <param name="defaultSortExpression">Default sort expression if sorting is null/empty</param>
    /// <param name="defaultDescending">Whether default sort is descending (default: false)</param>
    /// <returns>Sorted query</returns>
    public static IQueryable<T> ApplySorting<T, TKey>(
        this IQueryable<T> query,
        string? sorting,
        Expression<Func<T, TKey>> defaultSortExpression,
        bool defaultDescending = false)
    {
        if (!string.IsNullOrWhiteSpace(sorting))
        {
            return query.OrderBy(sorting);
        }

        return defaultDescending
            ? query.OrderByDescending(defaultSortExpression)
            : query.OrderBy(defaultSortExpression);
    }

    /// <summary>
    /// Applies sorting from ISortedResultRequest with a default sort expression
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TKey">Sort key type</typeparam>
    /// <param name="query">Source query</param>
    /// <param name="input">Request containing Sorting property</param>
    /// <param name="defaultSortExpression">Default sort expression</param>
    /// <param name="defaultDescending">Whether default sort is descending</param>
    /// <returns>Sorted query</returns>
    public static IQueryable<T> ApplySorting<T, TKey>(
        this IQueryable<T> query,
        ISortedResultRequest input,
        Expression<Func<T, TKey>> defaultSortExpression,
        bool defaultDescending = false)
    {
        return query.ApplySorting(input.Sorting, defaultSortExpression, defaultDescending);
    }

    /// <summary>
    /// Applies both sorting and paging in one call
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TKey">Sort key type</typeparam>
    /// <param name="query">Source query</param>
    /// <param name="input">Request containing both sorting and paging info</param>
    /// <param name="defaultSortExpression">Default sort expression</param>
    /// <param name="defaultDescending">Whether default sort is descending</param>
    /// <returns>Sorted and paged query</returns>
    public static IQueryable<T> ApplySortingAndPaging<T, TKey>(
        this IQueryable<T> query,
        IPagedAndSortedResultRequest input,
        Expression<Func<T, TKey>> defaultSortExpression,
        bool defaultDescending = false)
    {
        return query
            .ApplySorting(input.Sorting, defaultSortExpression, defaultDescending)
            .ApplyPaging(input);
    }

    /// <summary>
    /// Applies optional OrderBy with ThenBy for secondary sorting
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TKey1">Primary sort key type</typeparam>
    /// <typeparam name="TKey2">Secondary sort key type</typeparam>
    /// <param name="query">Source query</param>
    /// <param name="primarySort">Primary sort expression</param>
    /// <param name="secondarySort">Secondary sort expression</param>
    /// <returns>Sorted query</returns>
    public static IQueryable<T> OrderByThenBy<T, TKey1, TKey2>(
        this IQueryable<T> query,
        Expression<Func<T, TKey1>> primarySort,
        Expression<Func<T, TKey2>> secondarySort)
    {
        return query.OrderBy(primarySort).ThenBy(secondarySort);
    }

    /// <summary>
    /// Applies case-insensitive search filter using EF.Functions.Like for database-level search
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">Source query</param>
    /// <param name="searchTerm">Search term (will be wrapped with %)</param>
    /// <param name="predicate">Filter predicate</param>
    /// <returns>Filtered query</returns>
    public static IQueryable<T> WhereIfSearch<T>(
        this IQueryable<T> query,
        string? searchTerm,
        Expression<Func<T, bool>> predicate)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(predicate);
    }
}

/// <summary>
/// Helper class for building case-insensitive search expressions for EF Core
/// Uses ToLower().Contains() pattern which EF Core translates to LOWER() LIKE '%...%'
/// </summary>
public static class SearchHelper
{
    /// <summary>
    /// Checks if the field contains the search term (case-insensitive)
    /// For use in EF Core queries - translates to SQL LOWER(field) LIKE '%term%'
    /// </summary>
    public static bool ContainsSearch(string? field, string searchLower)
    {
        return field != null && field.ToLower().Contains(searchLower);
    }

    /// <summary>
    /// Prepares search term for case-insensitive comparison
    /// </summary>
    public static string PrepareSearch(string? search)
    {
        return search?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Checks if search term is valid (not null or whitespace)
    /// </summary>
    public static bool HasSearch(string? search)
    {
        return !string.IsNullOrWhiteSpace(search);
    }
}
