using WishesTracer.Infraestructure.Scraper;

namespace WishesTracer.InfraestructureTests.Scraper;

[TestFixture]
public class MercadoLibreStrategyTests
{
    private MercadoLibreStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MercadoLibreStrategy();
    }

    [TestCase("https://www.mercadolibre.com.mx/algo")]
    [TestCase("https://articulo.mercadolibre.com.ar/MLA-123-producto")]
    [TestCase("https://mercadolibre.com/producto")]
    public void CanHandle_WhenUrlIsFromMercadoLibre_ShouldReturnTrue(string url)
    {
        // Act
        var result = _strategy.CanHandle(url);

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("https://www.amazon.com.mx/gp/product/B09SVCLB79")]
    [TestCase("https://www.liverpool.com.mx/tienda/pdp/111")]
    public void CanHandle_WhenUrlIsNotFromMercadoLibre_ShouldReturnFalse(string url)
    {
        // Act
        var result = _strategy.CanHandle(url);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ParseHtml_WhenProductIsAvailable_ShouldExtractAllDataCorrectly()
    {
        // Arrange
        var solutionDir = AppDomain.CurrentDomain.BaseDirectory.Split(new[] { "/bin/" },
            StringSplitOptions.None)[0];
        var htmlPath = Path.Combine(solutionDir,
            "Scraper/Templates/ml/(2) Apple iPhone 16 (128 GB) - Negro _ Envío gratis.html");
        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        var url = "https://www.mercadolibre.com.mx/apple-iphone-16-128-gb-negro/p/MLM29329096";

        // Act
        var result = _strategy.ParseHtml(htmlContent, url);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Title, Is.EqualTo("Apple iPhone 16 (128 GB) - Negro"));
        Assert.That(result.Price, Is.EqualTo(13835.51m));
        Assert.That(result.Currency, Is.EqualTo("MXN"));
        Assert.That(result.IsAvailable, Is.True);
        Assert.That(result.Vendor, Is.EqualTo("MercadoLibre"));
        Assert.That(result.Url, Is.EqualTo(url));
    }

    [Test]
    public void ParseHtml_WhenHtmlIsEmpty_ShouldReturnDefaultDto()
    {
        // Arrange
        var htmlContent = "<html><body></body></html>";
        var url = "https://www.mercadolibre.com.mx/producto-vacio";

        // Act
        var result = _strategy.ParseHtml(htmlContent, url);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Title, Is.EqualTo("Título no encontrado"));
        Assert.That(result.Price, Is.EqualTo(0m));
        Assert.That(result.IsAvailable, Is.False);
    }

    [Test]
    public void ParseHtml_WhenPriceIsNotFound_ShouldReturnPriceZero()
    {
        // Arrange
        var htmlContent = "<html><body><h1>Producto sin precio</h1></body></html>";
        var url = "https://www.mercadolibre.com.mx/producto-sin-precio";

        // Act
        var result = _strategy.ParseHtml(htmlContent, url);

        // Assert
        Assert.That(result.Price, Is.EqualTo(0m));
        Assert.That(result.IsAvailable, Is.False);
    }

    [Test]
    public void ParseHtml_WhenTitleIsNotFound_ShouldReturnDefaultTitle()
    {
        // Arrange
        var htmlContent = "<html><body><meta itemprop='price' content='123.45'></body></html>";
        var url = "https://www.mercadolibre.com.mx/producto-sin-titulo";

        // Act
        var result = _strategy.ParseHtml(htmlContent, url);

        // Assert
        Assert.That(result.Title, Is.EqualTo("Título no encontrado"));
    }

    [Test]
    public void ParseHtml_WhenProductIsPaused_ShouldReturnNotAvailable()
    {
        // Arrange
        // Simulamos un HTML con el indicador de publicación pausada
        var htmlContent = """
                          <html>
                              <body>
                                  <h1 class='ui-pdp-title'>Producto Pausado</h1>
                                  <meta itemprop='price' content='999'>
                                  <div class='ui-pdp-promotions-pill-label--PAUSED'>Publicación pausada</div>
                              </body>
                          </html>
                          """;
        var url = "https://www.mercadolibre.com.mx/producto-pausado";

        // Act
        var result = _strategy.ParseHtml(htmlContent, url);

        // Assert
        Assert.That(result.IsAvailable, Is.False);
        Assert.That(result.Title, Is.EqualTo("Producto Pausado"));
        Assert.That(result.Price, Is.EqualTo(999m));
    }

    [TestCase("https://www.mercadolibre.com.mx/p/MLM123", "MXN")]
    [TestCase("https://articulo.mercadolibre.com.ar/MLA-123", "ARS")]
    [TestCase("https://www.mercadolibre.com.co/p/MCO123", "COP")]
    [TestCase("https://www.mercadolibre.cl/p/MLC123", "CLP")]
    [TestCase("https://www.mercadolibre.com.br/p/MLB123", "BRL")]
    [TestCase("https://www.mercadolibre.com/p/MLU123", "MXN")] // Default
    [TestCase("invalid-url", "MXN")] // Default on error
    public void DetectCurrencyFromUrl_ShouldReturnCorrectCurrency(string url, string expectedCurrency)
    {
        // Act
        var result = _strategy.ParseHtml("", url);

        // Assert
        Assert.That(result.Currency, Is.EqualTo(expectedCurrency));
    }
}
