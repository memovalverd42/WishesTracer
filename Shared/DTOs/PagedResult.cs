// PagedResult.cs - DTO for paginated API responses
namespace WishesTracer.Shared.DTOs;

/// <summary>
/// Represents a paginated result set for API responses.
/// </summary>
/// <typeparam name="T">The type of items in the result set</typeparam>
/// <remarks>
/// Provides pagination metadata along with the data to enable client-side pagination
/// controls. Includes helper properties for determining page navigation availability.
/// </remarks>
public class PagedResult<T>
{
    /// <summary>
    /// Gets the items for the current page.
    /// </summary>
    public List<T> Items { get; init; }
    
    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int Page { get; init; }
    
    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; init; }
    
    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }
    
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
    
    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Initializes a new instance of the PagedResult class.
    /// </summary>
    /// <param name="items">The items for the current page</param>
    /// <param name="page">The current page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="totalCount">The total number of items</param>
    public PagedResult(List<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}

