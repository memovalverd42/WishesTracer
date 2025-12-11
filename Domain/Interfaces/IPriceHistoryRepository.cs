// IPriceHistoryRepository.cs - Repository contract for price history data access
using WishesTracer.Domain.Entities;

namespace WishesTracer.Domain.Interfaces;

/// <summary>
/// Defines the repository contract for price history persistence operations.
/// </summary>
/// <remarks>
/// Provides data access abstraction for product price history records,
/// enabling retrieval of historical pricing data for trend analysis.
/// </remarks>
public interface IPriceHistoryRepository
{
    /// <summary>
    /// Retrieves all price history entries for a specific product.
    /// </summary>
    /// <param name="productId">The unique identifier of the product</param>
    /// <returns>A list of price history entries ordered by timestamp</returns>
    Task<List<PriceHistory>> GetHistoryByProductIdAsync(Guid productId);

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    Task SaveChangesAsync();
}
