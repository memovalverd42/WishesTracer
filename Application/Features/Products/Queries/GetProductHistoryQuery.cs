// GetProductHistoryQuery.cs - Query for retrieving product price history
using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Errors;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Queries;

/// <summary>
/// Query for retrieving the price history of a specific product.
/// </summary>
/// <param name="ProductId">The unique identifier of the product</param>
/// <remarks>
/// Returns historical price data points for trend analysis and price tracking.
/// Results are cached for 1 hour.
/// </remarks>
public record GetProductHistoryQuery(Guid ProductId) : IRequest<Result<List<PriceHistoryDto>>>, 
    ICacheableQuery
{
    /// <inheritdoc />
    public string CacheKey => $"product-history:{ProductId}";
    
    /// <inheritdoc />
    public TimeSpan? Expiration => TimeSpan.FromHours(1);
}

/// <summary>
/// Handles the GetProductHistoryQuery by retrieving price history from the repository.
/// </summary>
public class GetProductHistoryHandler : IRequestHandler<GetProductHistoryQuery, 
    Result<List<PriceHistoryDto>>>
{
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// Initializes a new instance of the GetProductHistoryHandler class.
    /// </summary>
    /// <param name="priceHistoryRepository">The price history repository</param>
    /// <param name="productRepository">The product repository for validation</param>
    public GetProductHistoryHandler(
        IPriceHistoryRepository priceHistoryRepository,
        IProductRepository productRepository)
    {
        _priceHistoryRepository = priceHistoryRepository;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Handles the query execution and returns price history.
    /// </summary>
    /// <param name="request">The query containing the product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price history on success, or NotFound error if product doesn't exist</returns>
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
