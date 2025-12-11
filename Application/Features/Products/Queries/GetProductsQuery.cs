using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.DTOs;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Queries;

public record GetProductsQuery(int Page = 1, int PageSize = 10, string? SearchTerm = null) 
    : IRequest<Result<PagedResult<ProductDto>>>, ICacheableQuery
{
    public string CacheKey => $"products_page{Page}_size{PageSize}_search{SearchTerm ?? "all"}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

public class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsHandler(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _productRepository.GetPagedAsync(
            request.Page, 
            request.PageSize, 
            request.SearchTerm);

        var dtos = products.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.CurrentPrice,
            p.Currency,
            p.IsActive
        )).ToList();

        var pagedResult = new PagedResult<ProductDto>(
            dtos,
            request.Page,
            request.PageSize,
            totalCount
        );

        return pagedResult;
    }
}
