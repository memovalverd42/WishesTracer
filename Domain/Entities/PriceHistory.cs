namespace WishesTracer.Domain.Entities;

public class PriceHistory
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime Timestamp { get; private set; }

    // EF Core requiere constructor vacío a veces, o configuración específica
    private PriceHistory()
    {
    }

    public PriceHistory(Guid productId, decimal price, DateTime timestamp)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Price = price;
        Timestamp = timestamp;
    }
}
