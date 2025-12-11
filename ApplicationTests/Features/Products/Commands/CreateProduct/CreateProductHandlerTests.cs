using FluentAssertions;
using Moq;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Features.Products.Commands.CreateProduct;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Errors;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.ApplicationTests.Features.Products.Commands.CreateProduct;

public class CreateProductHandlerTests
{
    private Mock<IProductRepository> _repositoryMock;
    private Mock<IScraperService> _scraperServiceMock;
    
    private CreateProductHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _scraperServiceMock = new Mock<IScraperService>();

        _handler = new CreateProductHandler(_repositoryMock.Object, _scraperServiceMock.Object);
    }

    [Test]
    public async Task Handle_Should_ScrapeUrl_And_SaveProduct_When_Called()
    {
        // --- ARRANGE ---
        var url = "https://amazon.com.mx/test-product";
        var command = new CreateProductCommand(url);
        
        var fakeScrapedData = new ProductScrapedDto(
            Title: "Test Product",
            Price: 1500.50m,
            Currency: "MXN",
            IsAvailable: true,
            Url: url,
            Vendor: "Amazon"
        );

        _scraperServiceMock
            .Setup(x => x.ScrapeProductAsync(It.IsAny<string>()))
            .ReturnsAsync(fakeScrapedData);

        // --- ACT ---
        var result = await _handler.Handle(command, CancellationToken.None);

        // --- ASSERT (Verificar resultados) ---
        
        var product = result.Value;
        
        product.Should().NotBeNull();
        product.Name.Should().Be("Test Product");
        product.Price.Should().Be(1500.50m);
        product.IsActive.Should().BeTrue();

        // 2. VERIFICACIÓN CRÍTICA: ¿Se llamó a la base de datos?
        // Verificamos que el método AddAsync del repositorio se ejecutó exactamente 1 vez
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
        _repositoryMock.Verify(x => x.ExistsWithUrlAsync(It.IsAny<string>()), Times.Once);
        
        // 3. Verificamos que se llamó al scraper
        _scraperServiceMock.Verify(x => x.ScrapeProductAsync(It.IsAny<string>()), Times.Once);
    }
    
    [Test]
    public async Task Handle_Should_ReturnDuplicateUrlError_When_ProductAlreadyExists()
    {
        // --- ARRANGE ---
        var url = "https://amazon.com.mx/test-product";
        var command = new CreateProductCommand(url);
        var cleanedUrl = "https://amazon.com.mx/test-product";

        _repositoryMock
            .Setup(x => x.ExistsWithUrlAsync(cleanedUrl))
            .ReturnsAsync(new Product("Existing Product", cleanedUrl, "Amazon"));

        // --- ACT ---
        var result = await _handler.Handle(command, CancellationToken.None);

        // --- ASSERT ---
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProductErrors.DuplicateUrl(cleanedUrl).Code);
        result.Error.Description.Should().Be(ProductErrors.DuplicateUrl(cleanedUrl).Description);
        result.Error.Type.Should().Be(ProductErrors.DuplicateUrl(cleanedUrl).Type);

        _repositoryMock.Verify(x => x.ExistsWithUrlAsync(cleanedUrl), Times.Once);
        _scraperServiceMock.Verify(x => x.ScrapeProductAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
    }
    
    [Test]
    public async Task Handle_Should_ReturnInvalidUrlError_When_UrlIsInvalid()
    {
        // --- ARRANGE ---
        var command = new CreateProductCommand("invalid-url");

        // --- ACT ---
        var result = await _handler.Handle(command, CancellationToken.None);

        // --- ASSERT ---
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProductErrors.InvalidUrl.Code);
        result.Error.Description.Should().Be(ProductErrors.InvalidUrl.Description);
        result.Error.Type.Should().Be(ProductErrors.InvalidUrl.Type);

        _repositoryMock.Verify(x => x.ExistsWithUrlAsync(It.IsAny<string>()), Times.Never);
        _scraperServiceMock.Verify(x => x.ScrapeProductAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
    }
    
    [Test]
    public async Task Handle_Should_ThrowException_When_ScraperServiceFails()
    {
        // --- ARRANGE ---
        var url = "https://amazon.com.mx/test-product";
        var command = new CreateProductCommand(url);
        var cleanedUrl = "https://amazon.com.mx/test-product";

        _scraperServiceMock
            .Setup(x => x.ScrapeProductAsync(cleanedUrl))
            .ThrowsAsync(new Exception("Scraper failed"));

        // --- ACT & ASSERT ---
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>().WithMessage("Scraper failed");

        _repositoryMock.Verify(x => x.ExistsWithUrlAsync(cleanedUrl), Times.Once);
        _scraperServiceMock.Verify(x => x.ScrapeProductAsync(cleanedUrl), Times.Once);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Never);
    }
}
