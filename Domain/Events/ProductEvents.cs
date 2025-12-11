using MediatR;

namespace WishesTracer.Domain.Events;

public record PriceChangedEvent(
    Guid ProductId, 
    string ProductName, 
    decimal OldPrice, 
    decimal NewPrice, 
    string Currency
) : INotification;
