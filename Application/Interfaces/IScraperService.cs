// IScraperService.cs - Contract for web scraping operations
using WishesTracer.Application.DTOs;

namespace WishesTracer.Application.Interfaces;

/// <summary>
/// Defines the contract for scraping product information from external websites.
/// </summary>
/// <remarks>
/// This service abstracts the complexity of web scraping, supporting multiple
/// vendors through a strategy pattern implementation.
/// </remarks>
public interface IScraperService
{
    /// <summary>
    /// Scrapes product information from the specified URL.
    /// </summary>
    /// <param name="url">The product URL to scrape</param>
    /// <returns>Scraped product data including title, price, and availability</returns>
    /// <exception cref="WishesTracer.Infraestructure.Scraper.Exceptions.UnsupportedVendorException">
    /// Thrown when the URL's vendor is not supported
    /// </exception>
    /// <exception cref="WishesTracer.Infraestructure.Scraper.Exceptions.ScraperException">
    /// Thrown when scraping fails due to network or parsing issues
    /// </exception>
    Task<ProductScrapedDto> ScrapeProductAsync(string url);
}
