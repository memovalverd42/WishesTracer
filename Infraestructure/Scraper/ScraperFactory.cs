namespace WishesTracer.Infraestructure.Scraper;

public class ScraperFactory
{
    private readonly IEnumerable<IScraperStrategy> _strategies;
    
    public ScraperFactory(IEnumerable<IScraperStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IScraperStrategy GetStrategy(string url)
    {
        // LINQ: Busca la primera estrategia que levante la mano diciendo "Yo puedo"
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(url));

        return strategy ?? throw new NotSupportedException($"No tenemos estrategia para la URL: {url}");
    }
}
