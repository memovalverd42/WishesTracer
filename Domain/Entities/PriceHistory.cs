// PriceHistory.cs - Domain entity for tracking historical price data
namespace WishesTracer.Domain.Entities;

/// <summary>
/// Represents a historical price point for a tracked product.
/// </summary>
/// <remarks>
/// This entity stores immutable snapshots of product prices at specific points in time,
/// enabling price trend analysis and change notifications.
/// </remarks>
public class PriceHistory
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Parameterless constructor required by Entity Framework Core.
    /// </summary>
    private PriceHistory()
    {
    }

    /// <summary>
    /// Creates a new price history entry for a product.
    /// </summary>
    /// <param name="productId">The ID of the product this price belongs to</param>
    /// <param name="price">The price value at this point in time</param>
    /// <param name="timestamp">When this price was recorded</param>
    public PriceHistory(Guid productId, decimal price, DateTime timestamp)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Price = price;
        Timestamp = timestamp;
    }
}
