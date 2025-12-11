using Microsoft.EntityFrameworkCore;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.Infraestructure.Persistence.Repositories;

public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<PriceHistory> _dbSet;

    public PriceHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.PriceHistories;
    }
    
    public async Task<List<PriceHistory>> GetHistoryByProductIdAsync(Guid productId)
    {
        return await _dbSet
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.Timestamp)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
