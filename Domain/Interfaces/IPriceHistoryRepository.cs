using WishesTracer.Domain.Entities;

namespace WishesTracer.Domain.Interfaces;

public interface IPriceHistoryRepository
{
    Task<List<PriceHistory>> GetHistoryByProductIdAsync(Guid productId);
    Task SaveChangesAsync();
}
