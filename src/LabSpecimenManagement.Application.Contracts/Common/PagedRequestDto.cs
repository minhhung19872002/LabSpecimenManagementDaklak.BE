using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Base paged request with page and limit parameters
/// </summary>
public class PagedRequestDto
{
    private int _page = 1;
    private int _limit = 10;

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 1000)]
    public int Limit
    {
        get => _limit;
        set => _limit = value < 1 ? 10 : (value > 1000 ? 1000 : value);
    }

    /// <summary>
    /// Sorting expression
    /// </summary>
    public string? Sorting { get; set; }

    /// <summary>
    /// Calculate skip count for pagination
    /// </summary>
    public int SkipCount => (Page - 1) * Limit;

    /// <summary>
    /// Get max result count (same as Limit)
    /// </summary>
    public int MaxResultCount => Limit;
}

/// <summary>
/// Paged request with search filter
/// </summary>
public class PagedAndFilteredRequestDto : PagedRequestDto
{
    /// <summary>
    /// Search/filter text
    /// </summary>
    public string? Filter { get; set; }
}
