// Product.cs - Core domain entity representing a tracked product
namespace WishesTracer.Domain.Entities;

/// <summary>
/// Represents a product being tracked for price monitoring across various vendors.
/// </summary>
/// <remarks>
/// This is a rich domain entity that encapsulates product information, pricing data,
/// and state management. It maintains a price history and enforces business rules
/// for price updates through domain methods.
/// </remarks>
public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Vendor { get; private set; } = string.Empty; // Amazon, ML

    // Datos de precio
    public decimal CurrentPrice { get; private set; }
    public string Currency { get; private set; } = "MXN";

    // Estado
    public bool IsAvailable { get; private set; }
    public bool IsActive { get; set; } = true; // Para "pausar" el tracking
    public DateTime? LastChecked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Relación con el historial (One-to-Many)
    private readonly List<PriceHistory> _priceHistory = [];
    public IReadOnlyCollection<PriceHistory> PriceHistory => _priceHistory.AsReadOnly();

    /// <summary>
    /// Parameterless constructor required by Entity Framework Core for entity materialization.
    /// </summary>
    private Product()
    {
    }

    /// <summary>
    /// Creates a new product instance with the specified information.
    /// </summary>
    /// <param name="name">The product name or title</param>
    /// <param name="url">The URL where the product can be found</param>
    /// <param name="vendor">The vendor or marketplace (e.g., Amazon, MercadoLibre)</param>
    public Product(string name, string url, string vendor)
    {
        Id = Guid.NewGuid();
        Name = name;
        Url = url;
        Vendor = vendor;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Updates the product's current price and availability status.
    /// Automatically records price changes in the history collection.
    /// </summary>
    /// <param name="newPrice">The new price value</param>
    /// <param name="currency">The currency code (e.g., MXN, USD)</param>
    /// <param name="isAvailable">Whether the product is currently available for purchase</param>
    /// <remarks>
    /// If the price has changed and is greater than zero, a new entry is added to the 
    /// price history before updating the current price.
    /// </remarks>
    public void UpdatePrice(decimal newPrice, string currency, bool isAvailable)
    {
        // Si el precio cambió, agregamos al historial
        if (CurrentPrice != newPrice && newPrice > 0)
        {
            _priceHistory.Add(new PriceHistory(Id, CurrentPrice, DateTime.UtcNow));
        }

        CurrentPrice = newPrice;
        Currency = currency;
        IsAvailable = isAvailable;
        LastChecked = DateTime.UtcNow;
    }
}
