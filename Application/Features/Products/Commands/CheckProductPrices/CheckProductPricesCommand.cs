using MediatR;
using Microsoft.Extensions.Logging;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.Application.Features.Products.Commands.CheckProductPrices;

public record CheckProductPricesCommand : IRequest;


public class CheckProductPricesHandler : IRequestHandler<CheckProductPricesCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IScraperService _scraperService;
    private readonly ILogger<CheckProductPricesHandler> _logger;

    public CheckProductPricesHandler(IProductRepository productRepository, IScraperService scraperService,
        ILogger<CheckProductPricesHandler> logger)
    {
        _productRepository = productRepository;
        _scraperService = scraperService;
        _logger = logger;
    }
    
    public async Task Handle(CheckProductPricesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("---- Iniciando ciclo de monitoreo de precios ---");

        var products = await _productRepository.GetAllActiveAsync();

        foreach (var product in products)
        {
            try
            {
                var scrapedData = await _scraperService.ScrapeProductAsync(product.Url);
                
                // Lógica de Negocio: Actualizar
                product.UpdatePrice(scrapedData.Price, scrapedData.Currency, scrapedData.IsAvailable);

                // Guardar cambios
                await _productRepository.UpdateAsync(product);
                
                _logger.LogInformation("Producto revisado: {Name} - Precio: {Price}", product.Name, product.CurrentPrice);
            }
            catch (Exception ex)
            {
                // IMPORTANTE: Catch dentro del loop.
                // Si falla un producto (ej. Amazon cambió el HTML), NO queremos que el Job se detenga
                // y deje de revisar los demás. Solo logueamos el error.
                _logger.LogError(ex, "Error al actualizar producto ID: {Id}", product.Id);
            }
        }
        
        _logger.LogInformation("--- Ciclo terminado ---");
    }
}
