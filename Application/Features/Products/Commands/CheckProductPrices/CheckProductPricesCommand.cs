using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Events;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.Application.Features.Products.Commands.CheckProductPrices;

public record CheckProductPricesCommand : IRequest;

public class CheckProductPricesHandler : IRequestHandler<CheckProductPricesCommand>
{
    private readonly ILogger<CheckProductPricesHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublisher _publisher;

    public CheckProductPricesHandler(IServiceScopeFactory scopeFactory,
        IPublisher publisher,
        ILogger<CheckProductPricesHandler> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _publisher = publisher;
    }

    public async Task Handle(CheckProductPricesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("---- Iniciando ciclo de monitoreo de precios ---");

        List<Guid> productIds;

        using (var scope = _scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            productIds = await repo.GetActiveProductIdsAsync();
        }

        foreach (var productId in productIds)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
                var scraper = scope.ServiceProvider.GetRequiredService<IScraperService>();

                try
                {
                    var product = await repo.GetByIdAsync(productId);
                    if (product is not { IsActive: true }) continue;

                    // 1. Scrapeo
                    var scrapedData = await scraper.ScrapeProductAsync(product.Url);

                    // 🛑 PASO A: CAPTURAR EL PRECIO ANTERIOR (MEMORIA)
                    // Necesitamos saber cuánto costaba hace 1 milisegundo para saber si hubo cambio
                    var oldPrice = product.CurrentPrice;

                    // 2. Actualización de Dominio (Aquí 'product.CurrentPrice' cambia al nuevo valor)
                    product.UpdatePrice(scrapedData.Price, scrapedData.Currency, scrapedData.IsAvailable);

                    // 🛑 PASO B: DETECTAR SI HUBO CAMBIO
                    // Comparamos la variable temporal 'oldPrice' con el estado actual de la entidad
                    bool hasPriceChanged = oldPrice != product.CurrentPrice;

                    // 3. Guardar (Persistir el cambio en Postgres)
                    await repo.SaveChangesAsync();

                    // 🛑 PASO C: PUBLICAR EL EVENTO
                    // Solo si detectamos que el precio cambió, avisamos al resto del sistema.
                    if (hasPriceChanged)
                    {
                        // Creamos el evento inmutable (record) con los datos del cambio
                        var domainEvent = new PriceChangedEvent(
                            product.Id, 
                            product.Name, 
                            oldPrice,              // Precio anterior
                            product.CurrentPrice,  // Precio nuevo
                            product.Currency
                        );

                        // MediatR busca a todos los que escuchen 'PriceChangedEvent' (ej. Email, SignalR)
                        await _publisher.Publish(domainEvent, cancellationToken);
                        
                        _logger.LogWarning("📢 Cambio de precio detectado para {Name}: {Old} -> {New}", product.Name, oldPrice, product.CurrentPrice);
                    }
                    else 
                    {
                        _logger.LogInformation("Producto revisado sin cambios: {Name}", product.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando producto {Id}", productId);
                }
            } 
        }
        
        _logger.LogInformation("---- Fin del ciclo de monitoreo de precios ---");
    }
}
