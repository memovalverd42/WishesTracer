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
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(List<Product> Products, int TotalCount)> GetPagedAsync(int page, int pageSize, string? searchTerm)
    {
        var query = _dbSet
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm) || p.Url.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (products, totalCount);
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

    public async Task<List<Guid>> GetActiveProductIdsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
