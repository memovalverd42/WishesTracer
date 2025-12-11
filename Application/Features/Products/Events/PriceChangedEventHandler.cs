using MediatR;
using Microsoft.Extensions.Logging;
using WishesTracer.Domain.Events;

namespace WishesTracer.Application.Features.Products.Events;

public class PriceChangedEventHandler : INotificationHandler<PriceChangedEvent>
{
    private readonly ILogger<PriceChangedEventHandler> _logger;

    public PriceChangedEventHandler(ILogger<PriceChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(PriceChangedEvent notification, CancellationToken cancellationToken)
    {
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
