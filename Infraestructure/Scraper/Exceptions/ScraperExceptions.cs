// ScraperExceptions.cs - Custom exceptions for web scraping operations
using WishesTracer.Shared.Exceptions;

namespace WishesTracer.Infraestructure.Scraper.Exceptions;

/// <summary>
/// Exception thrown when web scraping operations fail.
/// </summary>
/// <remarks>
/// This exception captures scraping failures including network issues, timeout errors,
/// or HTML structure changes that prevent data extraction.
/// </remarks>
public class ScrapingException : WishesTracerException
{
    /// <summary>
    /// Gets the URL that failed to be scraped.
    /// </summary>
    public string Url { get; }
    
    /// <summary>
    /// Gets the CSS selector that failed (if applicable).
    /// </summary>
    public string? Selector { get; }

    /// <summary>
    /// Initializes a new instance of the ScrapingException class.
    /// </summary>
    /// <param name="url">The URL that failed to be scraped</param>
    /// <param name="message">The error message</param>
    public ScrapingException(string url, string message) 
        : base($"Failed to scrape '{url}': {message}")
    {
        Url = url;
    }

    /// <summary>
    /// Initializes a new instance of the ScrapingException class with selector info.
    /// </summary>
    /// <param name="url">The URL that failed to be scraped</param>
    /// <param name="selector">The CSS selector that failed</param>
    /// <param name="message">The error message</param>
    public ScrapingException(string url, string selector, string message) 
        : base($"Failed to scrape '{url}' with selector '{selector}': {message}")
    {
        Url = url;
        Selector = selector;
    }
}

/// <summary>
/// Exception thrown when price extraction from scraped HTML fails.
/// </summary>
/// <remarks>
/// This occurs when the price element is found but cannot be parsed into a decimal value,
/// often due to unexpected formatting or currency symbols.
/// </remarks>
public class PriceExtractionException : WishesTracerException
{
    /// <summary>
    /// Gets the URL where price extraction failed.
    /// </summary>
    public string Url { get; }
    
    /// <summary>
    /// Gets the raw text value that couldn't be parsed as a price.
    /// </summary>
    public string? RawValue { get; }

    /// <summary>
    /// Initializes a new instance of the PriceExtractionException class.
    /// </summary>
    /// <param name="url">The URL where extraction failed</param>
    /// <param name="rawValue">The raw text that couldn't be parsed</param>
    /// <param name="message">The error message</param>
    public PriceExtractionException(string url, string? rawValue, string message) 
        : base($"Failed to extract price from '{url}': {message}")
    {
        Url = url;
        RawValue = rawValue;
    }
}
