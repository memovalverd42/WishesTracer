using FluentAssertions;
using Moq;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Features.Products.Commands.CreateProduct;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Entities;
using WishesTracer.Domain.Interfaces;

namespace WishesTracer.ApplicationTests.Features.Products.Commands.CreateProduct;

public class CreateProductHandlerTests
{
    // Mocks: Objetos falsos que controlamos nosotros
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IScraperService> _scraperServiceMock;
    
    // El SUT (System Under Test)
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests()
    {
        // 1. Inicializamos los Mocks
        _repositoryMock = new Mock<IProductRepository>();
        _scraperServiceMock = new Mock<IScraperService>();

        // 2. Inyectamos los Mocks en el Handler real
        _handler = new CreateProductHandler(_repositoryMock.Object, _scraperServiceMock.Object);
    }

    [Test] // [Test]
    public async Task Handle_Should_ScrapeUrl_And_SaveProduct_When_Called()
    {
        // --- ARRANGE (Preparar el escenario) ---
        var url = "https://amazon.com.mx/test-product";
        var command = new CreateProductCommand(url);

        // Simulamos qué responde el Scraper (sin llamar a Playwright de verdad)
        var fakeScrapedData = new ProductScrapedDto(
            Title: "Test Product",
            Price: 1500.50m,
            Currency: "MXN",
            IsAvailable: true,
            Url: url,
            Vendor: "Amazon"
        );

        _scraperServiceMock
            .Setup(x => x.ScrapeProductAsync(url))
            .ReturnsAsync(fakeScrapedData);

        // --- ACT (Ejecutar la acción) ---
        var result = await _handler.Handle(command, CancellationToken.None);

        // --- ASSERT (Verificar resultados) ---
        
        // 1. Verificamos que el resultado no sea nulo y tenga los datos correctos
        var product = result.Value;
        
        product.Should().NotBeNull();
        product.Name.Should().Be("Test Product");
        product.Price.Should().Be(1500.50m);
        product.IsActive.Should().BeTrue();

        // 2. VERIFICACIÓN CRÍTICA: ¿Se llamó a la base de datos?
        // Verificamos que el método AddAsync del repositorio se ejecutó exactamente 1 vez
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
        _repositoryMock.Verify(x => x.ExistsWithUrlAsync(url), Times.Once);
        
        // 3. Verificamos que se llamó al scraper
        _scraperServiceMock.Verify(x => x.ScrapeProductAsync(url), Times.Once);
    }
}
