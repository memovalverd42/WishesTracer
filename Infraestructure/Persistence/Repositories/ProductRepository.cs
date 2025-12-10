using WishesTracer.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.Infraestructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Product> _dbSet;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Products;
    }
    
    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(p => p.PriceHistory)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetAllActiveAsync()
    {
        return await _dbSet
            .Include(p => p.PriceHistory)
            .Where(p => p.IsActive)
            .ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _dbSet.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _dbSet.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task<Product?> ExistsWithUrlAsync(string url)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Url == url && p.IsActive);
    }
}
