using WishesTracer.Domain.Entities;

namespace WishesTracer.DomainTests.Entities;

[TestFixture]
public class ProductTests
{
    private const string ProductName = "Teclado Mecánico";
    private const string ProductUrl = "https://www.amazon.com.mx/dp/B09SVCLB79";
    private const string ProductVendor = "Amazon";

    [Test]
    public void Constructor_WhenCreatingANewProduct_ShouldInitializePropertiesCorrectly()
    {
        // Arrange & Act
        var product = new Product(ProductName, ProductUrl, ProductVendor);

        // Assert
        Assert.That(product.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(product.Name, Is.EqualTo(ProductName));
        Assert.That(product.Url, Is.EqualTo(ProductUrl));
        Assert.That(product.Vendor, Is.EqualTo(ProductVendor));
        Assert.That(product.IsActive, Is.True);
        Assert.That(product.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(product.PriceHistory, Is.Empty);
    }

    [Test]
    public void UpdatePrice_WhenPriceHasChangedAndIsPositive_ShouldUpdatePriceAndAddHistory()
    {
        // Arrange
        var product = new Product(ProductName, ProductUrl, ProductVendor);
        var initialPrice = product.CurrentPrice;
        var newPrice = 1500m;
        var currency = "MXN";
        var isAvailable = true;

        // Act
        product.UpdatePrice(newPrice, currency, isAvailable);

        // Assert
        Assert.That(product.CurrentPrice, Is.EqualTo(newPrice));
        Assert.That(product.Currency, Is.EqualTo(currency));
        Assert.That(product.IsAvailable, Is.EqualTo(isAvailable));
        Assert.That(product.LastChecked, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        Assert.That(product.PriceHistory, Has.Count.EqualTo(1));

        var historyEntry = product.PriceHistory.First();
        Assert.That(historyEntry.Price, Is.EqualTo(initialPrice));
        Assert.That(historyEntry.ProductId, Is.EqualTo(product.Id));
    }

    [Test]
    public void UpdatePrice_WhenPriceIsTheSame_ShouldUpdatePropertiesButNotAddHistory()
    {
        // Arrange
        var product = new Product(ProductName, ProductUrl, ProductVendor);
        var initialPrice = 1200m;
        product.UpdatePrice(initialPrice, "MXN", true); // Set initial price

        // Act
        product.UpdatePrice(initialPrice, "MXN", false); // Price is the same, availability changes

        // Assert
        Assert.That(product.CurrentPrice, Is.EqualTo(initialPrice));
        Assert.That(product.IsAvailable, Is.False); // Availability should be updated
        Assert.That(product.PriceHistory, Has.Count.EqualTo(1)); // Only the initial update
    }

    [Test]
    public void UpdatePrice_WhenNewPriceIsZero_ShouldUpdatePriceButNotAddHistory()
    {
        // Arrange
        var product = new Product(ProductName, ProductUrl, ProductVendor);
        product.UpdatePrice(1200m, "MXN", true); // Set initial price

        // Act
        product.UpdatePrice(0, "MXN", true);

        // Assert
        Assert.That(product.CurrentPrice, Is.EqualTo(0));
        Assert.That(product.PriceHistory, Has.Count.EqualTo(1)); // History should not be added for 0
    }

    [Test]
    public void UpdatePrice_WhenNewPriceIsNegative_ShouldUpdatePriceButNotAddHistory()
    {
        // Arrange
        var product = new Product(ProductName, ProductUrl, ProductVendor);
        product.UpdatePrice(1200m, "MXN", true); // Set initial price

        // Act
        product.UpdatePrice(-100m, "MXN", true);

        // Assert
        Assert.That(product.CurrentPrice, Is.EqualTo(-100m));
        Assert.That(product.PriceHistory, Has.Count.EqualTo(1)); // History should not be added for negative price
    }

    [Test]
    public void IsActive_Property_CanBeSet()
    {
        // Arrange
        var product = new Product(ProductName, ProductUrl, ProductVendor);

        // Act
        product.IsActive = false;

        // Assert
        Assert.That(product.IsActive, Is.False);
    }
}
