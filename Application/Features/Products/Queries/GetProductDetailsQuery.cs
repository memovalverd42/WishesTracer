using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Errors;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Queries;

public record GetProductDetailsQuery(Guid ProductId) : IRequest<Result<ProductDetailsDto>>, ICacheableQuery
{
    public string CacheKey => $"product-details:{ProductId}";
    public TimeSpan? Expiration => TimeSpan.FromHours(1);
}

public class GetProductDetailsHandler : IRequestHandler<GetProductDetailsQuery, Result<ProductDetailsDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductDetailsHandler(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductDetailsDto>> Handle(GetProductDetailsQuery request,
        CancellationToken cancellationToken)
    {
        // Validar que el producto exista
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return ProductErrors.NotFound(request.ProductId);

        var dto = new ProductDetailsDto(
            product.Id,
            product.Name,
            product.Url,
            product.Vendor,
            product.CurrentPrice,
            product.Currency,
            product.IsAvailable,
            product.IsActive,
            product.LastChecked,
            product.CreatedAt,
            product.PriceHistory.Select(h => new PriceHistoryDto(h.Price, h.Timestamp)).ToList()
        );

        return dto;
    }
}
