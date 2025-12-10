using WishesTracer.Application.DTOs;

namespace WishesTracer.Infraestructure.Scraper;

public interface IScraperStrategy
{
    bool CanHandle(string url);
    ProductScrapedDto ParseHtml(string html, string url);
}
