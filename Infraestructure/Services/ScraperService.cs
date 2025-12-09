using Infraestructure.Scraper;
using Infraestructure.Scraper.Core;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Interfaces;

namespace Infraestructure.Services;


public class ScraperService : IScraperService
{
    private readonly ScraperFactory _factory;
    private readonly PlaywrightEngine _engine;

    public ScraperService(ScraperFactory factory, PlaywrightEngine engine)
    {
        _factory = factory;
        _engine = engine;
    }

    public async Task<ProductScrapedDto> ScrapeProductAsync(string url)
    {
        // 1. Inicializar si es necesario (mejor hacerlo al inicio de la app)
        await _engine.InitializeAsync();

        // 2. Obtener estrategia
        var strategy = _factory.GetStrategy(url);

        // 3. Obtener HTML
        var html = await _engine.GetHtmlContentAsync(url);

        if (string.IsNullOrEmpty(html))
            throw new Exception("No se pudo obtener el HTML");

        // 4. Parsear
        return strategy.ParseHtml(html, url);
    }
}
