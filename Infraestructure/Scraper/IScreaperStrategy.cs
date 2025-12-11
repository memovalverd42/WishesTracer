// IScraperStrategy.cs - Strategy pattern interface for vendor-specific scraping
using WishesTracer.Application.DTOs;

namespace WishesTracer.Infraestructure.Scraper;

/// <summary>
/// Defines the contract for vendor-specific web scraping strategies.
/// </summary>
/// <remarks>
/// Implements the Strategy pattern to support multiple e-commerce vendors (Amazon,
/// MercadoLibre, etc.). Each strategy knows how to extract product data from its
/// vendor's HTML structure using CSS selectors.
/// </remarks>
public interface IScraperStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the specified URL.
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <returns>True if this strategy supports the URL's vendor; otherwise, false</returns>
    bool CanHandle(string url);

    /// <summary>
    /// Parses product information from HTML content.
    /// </summary>
    /// <param name="html">The HTML content to parse</param>
    /// <param name="url">The source URL (for vendor identification)</param>
    /// <returns>Extracted product data including title, price, and availability</returns>
    /// <exception cref="Exceptions.ScrapingException">
    /// Thrown when required elements cannot be found
    /// </exception>
    /// <exception cref="Exceptions.PriceExtractionException">
    /// Thrown when price parsing fails
    /// </exception>
    ProductScrapedDto ParseHtml(string html, string url);
}
