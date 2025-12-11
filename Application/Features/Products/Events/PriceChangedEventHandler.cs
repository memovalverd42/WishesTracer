// PriceChangedEventHandler.cs - Handler for price change domain events
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WishesTracer.Domain.Events;

namespace WishesTracer.Application.Features.Products.Events;

/// <summary>
/// Handles PriceChangedEvent notifications to perform cache invalidation and notifications.
/// </summary>
/// <remarks>
/// This event handler is decoupled from the price checking logic, enabling extensibility
/// for additional notifications (email, WhatsApp, WebSocket, etc.) without modifying
/// the core price monitoring workflow.
/// </remarks>
public class PriceChangedEventHandler : INotificationHandler<PriceChangedEvent>
{
    private readonly ILogger<PriceChangedEventHandler> _logger;
    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance of the PriceChangedEventHandler class.
    /// </summary>
    /// <param name="logger">Logger for price change notifications</param>
    /// <param name="cache">Distributed cache for invalidation</param>
    public PriceChangedEventHandler(ILogger<PriceChangedEventHandler> logger, 
        IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Handles the price changed event by invalidating related cache entries and logging.
    /// </summary>
    /// <param name="notification">The price changed event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <remarks>
    /// Invalidates both product history and product details caches to ensure fresh data
    /// is served after a price change. This is where additional notification logic
    /// (email, SMS, push notifications) can be implemented.
    /// </remarks>
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
