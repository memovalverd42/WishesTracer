// IProductRepository.cs - Repository contract for product data access
using WishesTracer.Domain.Entities;

namespace WishesTracer.Domain.Interfaces;

/// <summary>
/// Defines the repository contract for product persistence operations.
/// </summary>
/// <remarks>
/// This interface abstracts data access logic following the Repository pattern,
/// enabling the domain layer to remain independent of infrastructure concerns.
/// </remarks>
public interface IProductRepository
{
    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product's unique identifier</param>
    /// <returns>The product if found; otherwise, null</returns>
    Task<Product?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all products that are currently active for monitoring.
    /// </summary>
    /// <returns>A list of all active products</returns>
    Task<List<Product>> GetAllActiveAsync();

    /// <summary>
    /// Retrieves a paginated list of products with optional filtering.
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term to filter by name or URL</param>
    /// <returns>A tuple containing the products for the page and total count</returns>
    Task<(List<Product> Products, int TotalCount)> GetPagedAsync(int page, int pageSize, 
        string? searchTerm);

    /// <summary>
    /// Adds a new product to the repository.
    /// </summary>
    /// <param name="product">The product entity to add</param>
    Task AddAsync(Product product);

    /// <summary>
    /// Updates an existing product in the repository.
    /// </summary>
    /// <param name="product">The product entity with updated values</param>
    Task UpdateAsync(Product product);

    /// <summary>
    /// Checks if a product exists with the specified URL.
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <returns>The existing product if found; otherwise, null</returns>
    Task <Product?> ExistsWithUrlAsync(string url);

    /// <summary>
    /// Retrieves the IDs of all active products for monitoring.
    /// </summary>
    /// <returns>A list of product identifiers</returns>
    Task<List<Guid>> GetActiveProductIdsAsync();

    /// <summary>
    /// Persists all pending changes to the data store.
    /// </summary>
    Task SaveChangesAsync();
}
