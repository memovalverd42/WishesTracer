// GetProductDetailsQuery.cs - Query for retrieving detailed product information
using MediatR;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Errors;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Shared.Results;

namespace WishesTracer.Application.Features.Products.Queries;

/// <summary>
/// Query for retrieving detailed information about a specific product.
/// </summary>
/// <param name="ProductId">The unique identifier of the product</param>
/// <remarks>
/// This query includes the product's full details and price history.
/// Results are cached for 1 hour to reduce database load.
/// </remarks>
public record GetProductDetailsQuery(Guid ProductId) : IRequest<Result<ProductDetailsDto>>, 
    ICacheableQuery
{
    /// <inheritdoc />
    public string CacheKey => $"product-details:{ProductId}";
    
    /// <inheritdoc />
    public TimeSpan? Expiration => TimeSpan.FromHours(1);
}

/// <summary>
/// Handles the GetProductDetailsQuery by retrieving product details from the repository.
/// </summary>
public class GetProductDetailsHandler : IRequestHandler<GetProductDetailsQuery, 
    Result<ProductDetailsDto>>
{
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// Initializes a new instance of the GetProductDetailsHandler class.
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    public GetProductDetailsHandler(
        IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// Handles the query execution and returns product details.
    /// </summary>
    /// <param name="request">The query containing the product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details on success, or NotFound error if product doesn't exist</returns>
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
