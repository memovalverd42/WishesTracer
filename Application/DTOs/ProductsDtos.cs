// ProductsDtos.cs - Data Transfer Objects for product-related operations
namespace WishesTracer.Application.DTOs;

/// <summary>
/// Represents a simplified product view for list displays.
/// </summary>
/// <param name="Id">The unique identifier of the product</param>
/// <param name="Name">The product name or title</param>
/// <param name="Price">The current price of the product</param>
/// <param name="Currency">The currency code (e.g., MXN, USD)</param>
/// <param name="IsActive">Whether price monitoring is active for this product</param>
public record ProductDto(Guid Id, string Name, decimal Price, string Currency, bool IsActive);

/// <summary>
/// Represents detailed product information including price history.
/// </summary>
/// <param name="Id">The unique identifier of the product</param>
/// <param name="Name">The product name or title</param>
/// <param name="Url">The URL where the product can be found</param>
/// <param name="Vendor">The vendor or marketplace name</param>
/// <param name="CurrentPrice">The current price of the product</param>
/// <param name="Currency">The currency code (e.g., MXN, USD)</param>
/// <param name="IsAvailable">Whether the product is currently available</param>
/// <param name="IsActive">Whether price monitoring is active</param>
/// <param name="LastChecked">Timestamp of the last price check</param>
/// <param name="CreatedAt">Timestamp when the product was added to tracking</param>
/// <param name="PriceHistory">Historical price data for the product</param>
public record ProductDetailsDto(
    Guid Id,
    string Name,
    string Url,
    string Vendor,
    decimal CurrentPrice,
    string Currency,
    bool IsAvailable,
    bool IsActive,
    DateTime? LastChecked,
    DateTime CreatedAt,
    List<PriceHistoryDto> PriceHistory
);


/// <summary>
/// Represents scraped product data retrieved from external sources.
/// </summary>
/// <param name="Title">The product title from the vendor's website</param>
/// <param name="Price">The scraped price value</param>
/// <param name="Currency">The currency code</param>
/// <param name="IsAvailable">Whether the product is in stock</param>
/// <param name="Url">The URL that was scraped</param>
/// <param name="Vendor">The vendor name (e.g., "Amazon", "MercadoLibre")</param>
public record ProductScrapedDto(
    string Title,
    decimal Price,
    string Currency,
    bool IsAvailable,
    string Url,
    string Vendor
);
