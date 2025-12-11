// PriceHistoryDtos.cs - Data Transfer Object for price history records
namespace WishesTracer.Application.DTOs;

/// <summary>
/// Represents a historical price point for a product.
/// </summary>
/// <param name="Price">The price value at this point in time</param>
/// <param name="Timestamp">When this price was recorded</param>
public record PriceHistoryDto(decimal Price, DateTime Timestamp);
