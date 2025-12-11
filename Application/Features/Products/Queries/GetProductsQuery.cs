using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Queries;

public record GetProductsQuery() : IRequest<Result<List<ProductDto>>>, ICacheableQuery
{
    public string CacheKey => $"products";
    public TimeSpan? Expiration => TimeSpan.FromHours(1);
}

public class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<List<ProductDto>>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsHandler(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<List<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllActiveAsync();

        var dtos = products.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.CurrentPrice,
            p.Currency,
            p.IsActive
        )).ToList();

        return dtos;
    }
}
