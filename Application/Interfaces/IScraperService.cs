using WishesTracer.Application.DTOs;

namespace WishesTracer.Application.Interfaces;


public interface IScraperService
{
    Task<ProductScrapedDto> ScrapeProductAsync(string url);
}
