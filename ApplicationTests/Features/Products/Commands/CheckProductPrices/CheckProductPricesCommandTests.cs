using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Features.Products.Commands.CheckProductPrices;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Events;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.ApplicationTests.Features.Products.Commands.CheckProductPrices;

[TestFixture]
public class CheckProductPricesHandlerTests
{
    private Mock<IServiceScopeFactory> _scopeFactoryMock;
    private Mock<IPublisher> _publisherMock;
    private Mock<ILogger<CheckProductPricesHandler>> _loggerMock;
    private Mock<IProductRepository> _productRepositoryMock;
    private Mock<IScraperService> _scraperServiceMock;
    private CheckProductPricesHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _publisherMock = new Mock<IPublisher>();
        _loggerMock = new Mock<ILogger<CheckProductPricesHandler>>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _scraperServiceMock = new Mock<IScraperService>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IProductRepository))).Returns(_productRepositoryMock.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IScraperService))).Returns(_scraperServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(scopeMock.Object);

        _handler = new CheckProductPricesHandler(
            _scopeFactoryMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }
    
    [Test]
    public async Task Handle_ShouldLogInformationAndReturn_WhenNoActiveProducts()
    {
        // Arrange
        _productRepositoryMock.Setup(r => r.GetActiveProductIdsAsync()).ReturnsAsync(new List<Guid>());

        // Act
        await _handler.Handle(new CheckProductPricesCommand(), CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No hay productos activos para monitorear")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _productRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }
    
    [Test]
    public async Task Handle_ShouldProcessActiveProducts_WhenProductsAreFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product("Test Product", "https://test.com", "TestVendor");
        var scrapedData = new ProductScrapedDto("Test Product", 120, "USD", true, "https://test.com", "TestVendor");

        _productRepositoryMock.Setup(r => r.GetActiveProductIdsAsync()).ReturnsAsync(new List<Guid> { productId });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _scraperServiceMock.Setup(s => s.ScrapeProductAsync(product.Url)).ReturnsAsync(scrapedData);

        // Act
        await _handler.Handle(new CheckProductPricesCommand(), CancellationToken.None);

        // Assert
        _productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _publisherMock.Verify(p => p.Publish(It.IsAny<PriceChangedEvent>(), CancellationToken.None), Times.Once);
        Assert.That(product.CurrentPrice, Is.EqualTo(scrapedData.Price));
    }
    
    [Test]
    public async Task Handle_ShouldNotPublishEvent_WhenPriceHasNotChanged()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product("Test Product", "https://test.com", "TestVendor");
        product.UpdatePrice(120, "USD", true); // Set initial price
        var scrapedData = new ProductScrapedDto("Test Product", 120, "USD", true, "https://test.com", "TestVendor");

        _productRepositoryMock.Setup(r => r.GetActiveProductIdsAsync()).ReturnsAsync(new List<Guid> { productId });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _scraperServiceMock.Setup(s => s.ScrapeProductAsync(product.Url)).ReturnsAsync(scrapedData);

        // Act
        await _handler.Handle(new CheckProductPricesCommand(), CancellationToken.None);

        // Assert
        _productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _publisherMock.Verify(p => p.Publish(It.IsAny<PriceChangedEvent>(), CancellationToken.None), Times.Never);
    }
    
    [Test]
    public async Task Handle_ShouldLogWarning_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(r => r.GetActiveProductIdsAsync()).ReturnsAsync(new List<Guid> { productId });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product)null);

        // Act
        await _handler.Handle(new CheckProductPricesCommand(), CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Producto no encontrado")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _scraperServiceMock.Verify(s => s.ScrapeProductAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Test]
    public async Task Handle_ShouldLogError_WhenScrapingFails()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product("Test Product", "https://test.com", "TestVendor");
        _productRepositoryMock.Setup(r => r.GetActiveProductIdsAsync()).ReturnsAsync(new List<Guid> { productId });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _scraperServiceMock.Setup(s => s.ScrapeProductAsync(product.Url)).ThrowsAsync(new Exception("Scraping failed"));

        // Act
        await _handler.Handle(new CheckProductPricesCommand(), CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error scrapeando producto {product.Name} desde {product.Url}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        _publisherMock.Verify(p => p.Publish(It.IsAny<PriceChangedEvent>(), CancellationToken.None), Times.Never);
    }
    
    [Test]
    public async Task Handle_ShouldLogErrorAndThrow_WhenDatabaseSaveFails()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product("Test Product", "https://test.com", "TestVendor");
        var scrapedData = new ProductScrapedDto("Test Product", 120, "USD", true, "https://test.com", "TestVendor");
        var dbException = new Exception("Database error");

        _productRepositoryMock.Setup(r => r.GetActiveProductIdsAsync()).ReturnsAsync(new List<Guid> { productId });
        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _scraperServiceMock.Setup(s => s.ScrapeProductAsync(product.Url)).ReturnsAsync(scrapedData);
        _productRepositoryMock.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbException);

        // Act
        await _handler.Handle(new CheckProductPricesCommand(), CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error procesando producto")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
