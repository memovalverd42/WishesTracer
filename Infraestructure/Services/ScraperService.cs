using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Infraestructure.Scraper;
using WishesTracer.Infraestructure.Scraper.Core;

namespace WishesTracer.Infraestructure.Services;


public class ScraperService : IScraperService
{
    private readonly ScraperFactory _factory;
    private readonly PlaywrightEngine _engine;

    private static readonly SemaphoreSlim Semaphore = new(2, 2);

    public ScraperService(ScraperFactory factory, PlaywrightEngine engine)
    {
        _factory = factory;
        _engine = engine;
    }

    public async Task<ProductScrapedDto> ScrapeProductAsync(string url)
    {
        // Turno
        await Semaphore.WaitAsync();

        try
        {
            // Retardo aleatorio
            var randomDelay = Random.Shared.Next(2000, 5000);
            await Task.Delay(randomDelay);

            await _engine.InitializeAsync();
            var strategy = _factory.GetStrategy(url);
            
            var html = await _engine.GetHtmlContentAsync(url);
            
            if (string.IsNullOrEmpty(html))
                throw new Exception("HTML vacío");
            
            return strategy.ParseHtml(html, url);
        }
        finally
        {
            // Soltar el turno siempre, pase lo que pase
            Semaphore.Release();
        }
    }
}
