using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Errors;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(string Url) : IRequest<Result<ProductDto>>;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;

    // Aquí está el truco: Inyectamos interfaces, no implementaciones concretas
    private readonly IScraperService _scraperService;

    public CreateProductHandler(IProductRepository repository, IScraperService scraperService)
    {
        _repository = repository;
        _scraperService = scraperService;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Validar la URL
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var _))
            return ProductErrors.InvalidUrl;
        
        // Limpiar URL Dominio mas AbsolutePath (quitar todo el ruido de los query params)
        var uri = new Uri(request.Url);
        var cleanedUrl = "https://" + uri.Host + uri.AbsolutePath; 
        
        // Checar existencia de ese producto
        var exists = await _repository.ExistsWithUrlAsync(cleanedUrl);
        if (exists != null)
            return ProductErrors.DuplicateUrl(cleanedUrl);
        
        // Obtener datos actuales de la tienda (Amazon/ML)
        // El servicio decide qué estrategia usar internamente
        var scrapedData = await _scraperService.ScrapeProductAsync(cleanedUrl);
        
        // Validar que hayamos podido conseguir el precio
        if (scrapedData.Price <= 0)
            return ProductErrors.InvalidPrice;

        // 2. Crear la entidad de Dominio
        var product = new Product(scrapedData.Title, scrapedData.Url, scrapedData.Vendor);

        // 3. Establecer precio inicial (Lógica de dominio)
        product.UpdatePrice(scrapedData.Price, scrapedData.Currency, scrapedData.IsAvailable);

        // 4. Guardar en Base de Datos
        await _repository.AddAsync(product);

        // 5. Devolver DTO
        return new ProductDto(
            product.Id,
            product.Name,
            product.CurrentPrice,
            product.Currency,
            product.IsActive
        );
    }
}
