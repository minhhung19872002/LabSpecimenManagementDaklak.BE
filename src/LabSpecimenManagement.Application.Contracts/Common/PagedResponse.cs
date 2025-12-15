using System.Collections.Generic;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Pagination meta information
/// </summary>
public class PaginationMeta
{
    public long Total { get; set; }
    public int Limit { get; set; }
    public int Page { get; set; }
    public int TotalPages => Limit > 0 ? (int)System.Math.Ceiling((double)Total / Limit) : 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Paged response with items and meta information
/// </summary>
/// <typeparam name="T">Type of items</typeparam>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public PaginationMeta Meta { get; set; } = new();

    public PagedResponse()
    {
    }

    public PagedResponse(List<T> items, long total, int page, int limit)
    {
        Items = items;
        Meta = new PaginationMeta
        {
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    public static PagedResponse<T> Create(List<T> items, long total, int page, int limit)
    {
        return new PagedResponse<T>(items, total, page, limit);
    }
}

/// <summary>
/// API response with paged data
/// </summary>
/// <typeparam name="T">Type of items in the page</typeparam>
public class ApiPagedResponse<T> : ApiResponse<PagedResponse<T>>
{
    public static ApiPagedResponse<T> Ok(List<T> items, long total, int page, int limit)
    {
        return new ApiPagedResponse<T>
        {
            Data = PagedResponse<T>.Create(items, total, page, limit),
            Success = true,
            StatusCode = 200,
            Errors = null
        };
    }

    public new static ApiPagedResponse<T> Fail(Dictionary<string, List<string>> errors, int statusCode = 400)
    {
        return new ApiPagedResponse<T>
        {
            Data = null,
            Success = false,
            StatusCode = statusCode,
            Errors = errors
        };
    }

    public new static ApiPagedResponse<T> Fail(string field, string message, int statusCode = 400)
    {
        return new ApiPagedResponse<T>
        {
            Data = null,
            Success = false,
            StatusCode = statusCode,
            Errors = new Dictionary<string, List<string>>
            {
                { field, new List<string> { message } }
            }
        };
    }
}
