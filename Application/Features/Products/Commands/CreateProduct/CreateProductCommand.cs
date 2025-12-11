// CreateProductCommand.cs - Command for creating a new tracked product
using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Errors;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Command for creating a new product to track for price monitoring.
/// </summary>
/// <param name="Url">The URL of the product to track</param>
/// <remarks>
/// The URL will be cleaned (removing query parameters) and validated before creating
/// the product. The system will scrape initial product data from the vendor's website.
/// </remarks>
public record CreateProductCommand(string Url) : IRequest<Result<ProductDto>>;

/// <summary>
/// Handles the CreateProductCommand by scraping product data and persisting it.
/// </summary>
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IScraperService _scraperService;

    /// <summary>
    /// Initializes a new instance of the CreateProductHandler class.
    /// </summary>
    /// <param name="repository">The product repository</param>
    /// <param name="scraperService">The scraper service for fetching product data</param>
    public CreateProductHandler(IProductRepository repository, IScraperService scraperService)
    {
        _repository = repository;
        _scraperService = scraperService;
    }

    /// <summary>
    /// Handles the command execution, validates URL, scrapes data, and creates product.
    /// </summary>
    /// <param name="request">The command containing the product URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created product on success, or validation/conflict errors</returns>
    /// <exception cref="Infraestructure.Scraper.Exceptions.UnsupportedVendorException">
    /// Thrown when the URL's vendor is not supported
    /// </exception>
    /// <exception cref="Infraestructure.Scraper.Exceptions.ScraperException">
    /// Thrown when scraping fails
    /// </exception>
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, 
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var _))
            return ProductErrors.InvalidUrl;
        
        // Limpiar URL Dominio mas AbsolutePath (quitar todo el ruido de los query params)
        var uri = new Uri(request.Url);
        var cleanedUrl = "https://" + uri.Host + uri.AbsolutePath; 
        
        var exists = await _repository.ExistsWithUrlAsync(cleanedUrl);
        if (exists != null)
            return ProductErrors.DuplicateUrl(cleanedUrl);
        
        var scrapedData = await _scraperService.ScrapeProductAsync(cleanedUrl);
        
        if (scrapedData.Price <= 0)
            return ProductErrors.InvalidPrice;

        var product = new Product(scrapedData.Title, scrapedData.Url, scrapedData.Vendor);

        // 3. Establecer precio inicial (Lógica de dominio)
        product.UpdatePrice(scrapedData.Price, scrapedData.Currency, scrapedData.IsAvailable);

        await _repository.AddAsync(product);

        return new ProductDto(
            product.Id,
            product.Name,
            product.CurrentPrice,
            product.Currency,
            product.IsActive
        );
    }
}
