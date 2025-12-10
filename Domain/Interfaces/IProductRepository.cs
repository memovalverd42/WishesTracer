using WishesTracer.Domain.Entities;

namespace WishesTracer.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetAllActiveAsync();
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task <Product?> ExistsWithUrlAsync(string url);
}
