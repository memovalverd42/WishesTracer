using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Events;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.Application.Features.Products.Commands.CheckProductPrices;

public record CheckProductPricesCommand : IRequest;

public class CheckProductPricesHandler : IRequestHandler<CheckProductPricesCommand>
{
    private readonly ILogger<CheckProductPricesHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublisher _publisher;

    public CheckProductPricesHandler(
        IServiceScopeFactory scopeFactory,
        IPublisher publisher,
        ILogger<CheckProductPricesHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task Handle(CheckProductPricesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("---- Iniciando ciclo de monitoreo de precios ---");

        var activeProductIds = await GetActiveProductIdsAsync();
        
        if (activeProductIds.Count == 0)
        {
            _logger.LogInformation("No hay productos activos para monitorear");
            return;
        }

        await ProcessProductsAsync(activeProductIds, cancellationToken);
        
        _logger.LogInformation("---- Fin del ciclo de monitoreo de precios ---");
    }

    private async Task<List<Guid>> GetActiveProductIdsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        return await productRepository.GetActiveProductIdsAsync();
    }

    private async Task ProcessProductsAsync(List<Guid> productIds, CancellationToken cancellationToken)
    {
        foreach (var productId in productIds)
        {
            await ProcessSingleProductAsync(productId, cancellationToken);
        }
    }

    private async Task ProcessSingleProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var scraperService = scope.ServiceProvider.GetRequiredService<IScraperService>();

        try
        {
            await CheckAndUpdateProductPriceAsync(
                productId, 
                productRepository, 
                scraperService, 
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando producto {ProductId}", productId);
        }
    }

    private async Task CheckAndUpdateProductPriceAsync(
        Guid productId,
        IProductRepository productRepository,
        IScraperService scraperService,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(productId);
        
        if (!IsProductValidForPriceCheck(product))
        {
            return;
        }
        
        var scrapedProductData = await ScrapeProductDataAsync(scraperService, product!);
        
        if (scrapedProductData == null)
        {
            return;
        }

        var previousPrice = product!.CurrentPrice;
        
        UpdateProductWithScrapedData(product, scrapedProductData);
        
        await SaveProductChangesAsync(productRepository);
        
        await NotifyPriceChangeIfNeededAsync(product, previousPrice, cancellationToken);
    }

    private bool IsProductValidForPriceCheck(Product? product)
    {
        if (product == null)
        {
            _logger.LogWarning("Producto no encontrado");
            return false;
        }

        if (product.IsActive) return true;
        
        _logger.LogDebug("Producto {ProductId} está inactivo, omitiendo", product.Id);
        return false;
    }

    private async Task<ProductScrapedDto?> ScrapeProductDataAsync(IScraperService scraperService, Product product)
    {
        try
        {
            return await scraperService.ScrapeProductAsync(product.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scrapeando producto {ProductName} desde {Url}", 
                product.Name, product.Url);
            return null;
        }
    }

    private void UpdateProductWithScrapedData(Product product, ProductScrapedDto scrapedData)
    {
        product.UpdatePrice(scrapedData.Price, scrapedData.Currency, scrapedData.IsAvailable);
    }

    private async Task SaveProductChangesAsync(IProductRepository productRepository)
    {
        try
        {
            await productRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persistiendo cambios en la base de datos");
            throw;
        }
    }

    private async Task NotifyPriceChangeIfNeededAsync(
        Product product, 
        decimal previousPrice, 
        CancellationToken cancellationToken)
    {
        if (!HasPriceChanged(previousPrice, product.CurrentPrice))
        {
            _logger.LogInformation("Producto revisado sin cambios: {ProductName}", product.Name);
            return;
        }

        await PublishPriceChangedEventAsync(product, previousPrice, cancellationToken);
    }

    private bool HasPriceChanged(decimal previousPrice, decimal currentPrice)
    {
        return previousPrice != currentPrice;
    }

    private async Task PublishPriceChangedEventAsync(
        Product product, 
        decimal previousPrice, 
        CancellationToken cancellationToken)
    {
        var priceChangedEvent = CreatePriceChangedEvent(product, previousPrice);
        
        await _publisher.Publish(priceChangedEvent, cancellationToken);
        
        _logger.LogWarning(
            "📢 Cambio de precio detectado para {ProductName}: {PreviousPrice} -> {CurrentPrice}",
            product.Name, 
            previousPrice, 
            product.CurrentPrice);
    }

    private PriceChangedEvent CreatePriceChangedEvent(Product product, decimal previousPrice)
    {
        return new PriceChangedEvent(
            product.Id,
            product.Name,
            previousPrice,
            product.CurrentPrice,
            product.Currency
        );
    }
}
