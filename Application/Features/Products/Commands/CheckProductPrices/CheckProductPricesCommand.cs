using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.Application.Features.Products.Commands.CheckProductPrices;

public record CheckProductPricesCommand : IRequest;

public class CheckProductPricesHandler : IRequestHandler<CheckProductPricesCommand>
{
    private readonly ILogger<CheckProductPricesHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CheckProductPricesHandler(IServiceScopeFactory scopeFactory,
        ILogger<CheckProductPricesHandler> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task Handle(CheckProductPricesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("---- Iniciando ciclo de monitoreo de precios ---");

        List<Guid> productIds;

        // Obtener solo los IDs en un scope rápido
        // Esto es muy ligero y rápido.
        using (var scope = _scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            productIds = await repo.GetActiveProductIdsAsync();
        }

        foreach (var productId in productIds)
        {
            // Crear un "Unidad de Trabajo" aislada para ESTE producto
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
                var scraper = scope.ServiceProvider.GetRequiredService<IScraperService>();

                try
                {
                    // Cargar datos FRESCOS de la BD
                    var product = await repo.GetByIdAsync(productId);

                    if (product is not { IsActive: true }) continue;

                    // Scrapeo
                    var scrapedData = await scraper.ScrapeProductAsync(product.Url);

                    // Actualización de Dominio
                    product.UpdatePrice(scrapedData.Price, scrapedData.Currency, scrapedData.IsAvailable);

                    // D. Guardar
                    await repo.SaveChangesAsync();

                    _logger.LogInformation("Producto actualizado: {Name} - Precio: {Price}", product.Name,
                        product.CurrentPrice);
                }
                catch (Exception ex)
                {
                    // Si falla este producto, el error muere aquí y el bucle sigue con el siguiente.
                    _logger.LogError(ex, "Error procesando producto {Id}", productId);
                }
            } 
        }
        
        _logger.LogInformation("---- Fin del ciclo de monitoreo de precios ---");
    }
}
