using WishesTracer.Domain.Entities;

namespace WishesTracer.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetAllActiveAsync();
    Task<(List<Product> Products, int TotalCount)> GetPagedAsync(int page, int pageSize, string? searchTerm);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task <Product?> ExistsWithUrlAsync(string url);
    Task<List<Guid>> GetActiveProductIdsAsync();
    Task SaveChangesAsync();
}
