using WishesTracer.Infraestructure.Scraper;

namespace WishesTracer.InfraestructureTests.Scraper;

[TestFixture]
public class AmazonScraperStrategyTests
{
    private AmazonStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new AmazonStrategy();
    }

    [TestCase("https://www.amazon.com.mx/gp/product/B09SVCLB79")]
    [TestCase("https://www.amazon.com/dp/B09SVCLB79")]
    [TestCase("https://amazon.es/dp/B09SVCLB79")]
    public void CanHandle_WhenUrlIsFromAmazon_ShouldReturnTrue(string url)
    {
        // Act
        var result = _strategy.CanHandle(url);

        // Assert
        Assert.That(result, Is.True);
    }

    [TestCase("https://www.mercadolibre.com.mx/algo")]
    [TestCase("https://www.liverpool.com.mx/tienda/pdp/111")]
    public void CanHandle_WhenUrlIsNotFromAmazon_ShouldReturnFalse(string url)
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
            "Scraper/Templates/amazon/Google Pixel Buds Pro 2 Porcelana GA05760-JP Pequeño _ Amazon.com.mx_ Electrónicos.html");
        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        var url = "https://www.amazon.com.mx/Google-Pixel-Buds-Pro-Pequen%CC%83o/dp/B0CH32223C";

        // Act
        var result = _strategy.ParseHtml(htmlContent, url);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Title, Is.EqualTo("Google Pixel Buds Pro 2 Porcelana GA05760-JP Pequeño"));
        Assert.That(result.Price, Is.EqualTo(5165.49m));
        Assert.That(result.Currency, Is.EqualTo("MXN"));
        Assert.That(result.IsAvailable, Is.True);
        Assert.That(result.Vendor, Is.EqualTo("Amazon"));
        Assert.That(result.Url, Is.EqualTo(url));
    }

    [Test]
    public void ParseHtml_WhenHtmlIsEmpty_ShouldReturnDefaultDto()
    {
        // Arrange
        var htmlContent = "<html><body></body></html>";
        var url = "https://www.amazon.com.mx/producto-vacio";

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
        var htmlContent = "<html><body><span id='productTitle'>Producto sin precio</span></body></html>";
        var url = "https://www.amazon.com.mx/producto-sin-precio";

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
        var htmlContent = "<html><body><div class='twister-plus-buying-options-price-data'></div></body></html>";
        var url = "https://www.amazon.com.mx/producto-sin-titulo";

        // Act
        var result = _strategy.ParseHtml(htmlContent, url);

        // Assert
        Assert.That(result.Title, Is.EqualTo("Título no encontrado"));
    }

    [Test]
    public void ParseHtml_WhenProductIsNotAvailable_ShouldReturnNotAvailable()
    {
        // Arrange
        var htmlContent = """
                          <html>
                              <body>
                                  <span id='productTitle'>Producto No Disponible</span>
                                  <div id='availability'>
                                      <span class='a-color-price'>No disponible por el momento.</span>
                                  </div>
                              </body>
                          </html>
                          """;
        var url = "https://www.amazon.com.mx/producto-no-disponible";

        // Act
        var result = _strategy.ParseHtml(htmlContent, url);

        // Assert
        Assert.That(result.IsAvailable, Is.False);
        Assert.That(result.Title, Is.EqualTo("Producto No Disponible"));
    }

    [TestCase("https://www.amazon.com.mx/p/B09SVCLB79", "MXN")]
    [TestCase("https://www.amazon.com/dp/B09SVCLB79", "USD")]
    [TestCase("https://www.amazon.es/dp/B09SVCLB79", "EUR")]
    [TestCase("https://www.amazon.co.uk/dp/B09SVCLB79", "GBP")]
    [TestCase("https://www.amazon.com.br/dp/B09SVCLB79", "BRL")]
    [TestCase("invalid-url", "USD")] // Default on error
    public void DetectCurrencyFromUrl_ShouldReturnCorrectCurrency(string url, string expectedCurrency)
    {
        // Act
        var result = _strategy.ParseHtml("", url);

        // Assert
        Assert.That(result.Currency, Is.EqualTo(expectedCurrency));
    }
}
