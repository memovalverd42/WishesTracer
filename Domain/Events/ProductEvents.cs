// ProductEvents.cs - Domain events for product-related state changes
using MediatR;

namespace WishesTracer.Domain.Events;

/// <summary>
/// Domain event raised when a product's price changes.
/// </summary>
/// <remarks>
/// This event follows the MediatR notification pattern and is published when the price
/// monitoring system detects a price change. Event handlers can respond to this by sending
/// notifications, logging, updating analytics, etc.
/// </remarks>
/// <param name="ProductId">The unique identifier of the product whose price changed</param>
/// <param name="ProductName">The name of the product for reference</param>
/// <param name="OldPrice">The previous price value</param>
/// <param name="NewPrice">The new current price value</param>
/// <param name="Currency">The currency code (e.g., MXN, USD)</param>
public record PriceChangedEvent(
    Guid ProductId, 
    string ProductName, 
    decimal OldPrice, 
    decimal NewPrice, 
    string Currency
) : INotification;
