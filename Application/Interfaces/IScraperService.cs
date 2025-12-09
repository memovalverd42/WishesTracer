using Application.DTOs;

namespace Application.Interfaces;


public interface IScraperService
{
    Task<ProductScrapedDto> ScrapeProductAsync(string url);
}
