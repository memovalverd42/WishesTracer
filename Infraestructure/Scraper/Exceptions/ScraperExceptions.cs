using WishesTracer.Shared.Exceptions;

namespace WishesTracer.Infraestructure.Scraper.Exceptions;

public class ScrapingException : WishesTracerException
{
    public string Url { get; }
    public string? Selector { get; }

    public ScrapingException(string url, string message) 
        : base($"Failed to scrape '{url}': {message}")
    {
        Url = url;
    }

    public ScrapingException(string url, string selector, string message) 
        : base($"Failed to scrape '{url}' with selector '{selector}': {message}")
    {
        Url = url;
        Selector = selector;
    }
}

public class PriceExtractionException : WishesTracerException
{
    public string Url { get; }
    public string? RawValue { get; }

    public PriceExtractionException(string url, string? rawValue, string message) 
        : base($"Failed to extract price from '{url}': {message}")
    {
        Url = url;
        RawValue = rawValue;
    }
}
