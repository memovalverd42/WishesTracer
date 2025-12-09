namespace WishesTracer.Domain.Entities;

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

    // Constructor vacío requerido por EF Core
    private Product()
    {
    }

    // Constructor para crear uno nuevo
    public Product(string name, string url, string vendor)
    {
        Id = Guid.NewGuid();
        Name = name;
        Url = url;
        Vendor = vendor;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    // Comportamiento del Dominio (La lógica "rica")
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
