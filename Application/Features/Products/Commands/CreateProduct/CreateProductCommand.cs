using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(string Url) : IRequest<ProductDto>;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;

    // Aquí está el truco: Inyectamos interfaces, no implementaciones concretas
    private readonly IScraperService _scraperService;

    public CreateProductHandler(IProductRepository repository, IScraperService scraperService)
    {
        _repository = repository;
        _scraperService = scraperService;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener datos actuales de la tienda (Amazon/ML)
        // El servicio decide qué estrategia usar internamente
        var scrapedData = await _scraperService.ScrapeProductAsync(request.Url);

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
