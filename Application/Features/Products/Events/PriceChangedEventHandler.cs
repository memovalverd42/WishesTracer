using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WishesTracer.Domain.Events;

namespace WishesTracer.Application.Features.Products.Events;

public class PriceChangedEventHandler : INotificationHandler<PriceChangedEvent>
{
    private readonly ILogger<PriceChangedEventHandler> _logger;
    private readonly IDistributedCache _cache;

    public PriceChangedEventHandler(ILogger<PriceChangedEventHandler> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task Handle(PriceChangedEvent notification, CancellationToken cancellationToken)
    {
        var cacheKey = $"product-history:{notification.ProductId}";

        await _cache.RemoveAsync(cacheKey, cancellationToken);
        await _cache.RemoveAsync($"product-details:{notification.ProductId}", cancellationToken);
        
        // Lógica desacoplada: Aquí podrías mandar un correo, un WhatsApp o un WebSocket
        _logger.LogWarning(
            "🔔 ¡ALERTA! El producto '{Name}' cambió de precio. De {Old} a {New} {Currency}",
            notification.ProductName,
            notification.OldPrice,
            notification.NewPrice,
            notification.Currency
        );

        await Task.CompletedTask;
    }
}
