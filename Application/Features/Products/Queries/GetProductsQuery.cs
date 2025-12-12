// GetProductsQuery.cs - Query for retrieving paginated product list
using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.DTOs;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Queries;

/// <summary>
/// Query for retrieving a paginated list of products with optional filtering.
/// </summary>
/// <param name="Page">The page number (1-based, default: 1)</param>
/// <param name="PageSize">Number of items per page (default: 10, max: 100)</param>
/// <param name="SearchTerm">Optional search term to filter products by name or URL</param>
/// <remarks>
/// This query supports distributed caching with a 5-minute expiration time.
/// Results are cached based on pagination and search parameters.
/// </remarks>
public record GetProductsQuery(int Page = 1, int PageSize = 10, string? SearchTerm = null) 
    : IRequest<Result<PagedResult<ProductDto>>>, ICacheableQuery
{
    /// <inheritdoc />
    public string CacheKey => $"products_page{Page}_size{PageSize}_search{SearchTerm ?? "all"}";
    
    /// <inheritdoc />
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

/// <summary>
/// Handles the GetProductsQuery by retrieving products from the repository.
/// </summary>
public class GetProductsHandler : IRequestHandler<GetProductsQuery, 
    Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// Initializes a new instance of the GetProductsHandler class.
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    public GetProductsHandler(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// Handles the query execution and returns paginated product results.
    /// </summary>
    /// <param name="request">The query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A successful result containing paginated product data</returns>
    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, 
        CancellationToken cancellationToken)
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
