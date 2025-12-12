// ScraperService.cs - Service for orchestrating web scraping operations
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;
using WishesTracer.Infraestructure.Scraper;
using WishesTracer.Infraestructure.Scraper.Core;

namespace WishesTracer.Infraestructure.Services;

/// <summary>
/// Orchestrates web scraping operations with rate limiting and vendor strategy selection.
/// </summary>
/// <remarks>
/// This service coordinates between the Playwright browser engine and vendor-specific
/// scraping strategies. It implements rate limiting (max 2 concurrent requests) and
/// random delays to avoid being blocked by target websites.
/// </remarks>
public class ScraperService : IScraperService
{
    private readonly ScraperFactory _factory;
    private readonly PlaywrightEngine _engine;

    private static readonly SemaphoreSlim Semaphore = new(2, 2);

    /// <summary>
    /// Initializes a new instance of the ScraperService class.
    /// </summary>
    /// <param name="factory">Factory for obtaining vendor-specific scraping strategies</param>
    /// <param name="engine">Playwright browser engine for fetching HTML</param>
    public ScraperService(ScraperFactory factory, PlaywrightEngine engine)
    {
        _factory = factory;
        _engine = engine;
    }

    /// <summary>
    /// Scrapes product information from the specified URL.
    /// </summary>
    /// <param name="url">The product URL to scrape</param>
    /// <returns>Scraped product data including title, price, and availability</returns>
    /// <exception cref="Exceptions.ScraperExceptions.UnsupportedVendorException">
    /// Thrown when no strategy supports the URL's vendor
    /// </exception>
    /// <exception cref="Exceptions.ScrapingException">
    /// Thrown when scraping fails
    /// </exception>
    /// <remarks>
    /// This method is rate-limited to 2 concurrent requests and adds random delays
    /// (2-5 seconds) between requests to mimic human behavior and avoid detection.
    /// </remarks>
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
