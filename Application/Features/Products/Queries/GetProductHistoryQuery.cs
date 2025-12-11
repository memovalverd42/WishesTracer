using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Errors;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Queries;

public record GetProductHistoryQuery(Guid ProductId) : IRequest<Result<List<PriceHistoryDto>>> , ICacheableQuery
{
    public string CacheKey => $"product-history:{ProductId}";
    public TimeSpan? Expiration => TimeSpan.FromHours(1);
}

public class GetProductHistoryHandler : IRequestHandler<GetProductHistoryQuery, Result<List<PriceHistoryDto>>>
{
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IProductRepository _productRepository;

    public GetProductHistoryHandler(
        IPriceHistoryRepository priceHistoryRepository,
        IProductRepository productRepository)
    {
        _priceHistoryRepository = priceHistoryRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<List<PriceHistoryDto>>> Handle(GetProductHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // Validar que el producto exista
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return ProductErrors.NotFound(request.ProductId);
        
        var history = await _priceHistoryRepository.GetHistoryByProductIdAsync(request.ProductId);

        return history.Select(h => new PriceHistoryDto(h.Price, h.Timestamp)).ToList();
    }
}
