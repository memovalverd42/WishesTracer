// ICacheableQuery.cs - Contract for cacheable query operations
namespace WishesTracer.Application.Interfaces;

/// <summary>
/// Marker interface for queries that support distributed caching.
/// </summary>
/// <remarks>
/// Queries implementing this interface will have their results cached in Redis
/// through the CachingBehavior pipeline. This improves performance for frequently
/// accessed read-only data.
/// </remarks>
public interface ICacheableQuery
{
    /// <summary>
    /// Gets the unique cache key for this query.
    /// </summary>
    /// <remarks>
    /// The key should incorporate all query parameters to ensure proper cache isolation.
    /// </remarks>
    string CacheKey { get; }

    /// <summary>
    /// Gets the cache expiration time. If null, a default expiration will be used.
    /// </summary>
    TimeSpan? Expiration { get; }
}
