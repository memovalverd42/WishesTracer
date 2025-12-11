namespace WishesTracer.Application.DTOs;

public record ProductDto(Guid Id, string Name, decimal Price, string Currency, bool IsActive);

public record ProductDetailsDto(
    Guid Id,
    string Name,
    string Url,
    string Vendor,
    decimal CurrentPrice,
    string Currency,
    bool IsAvailable,
    bool IsActive,
    DateTime? LastChecked,
    DateTime CreatedAt,
    List<PriceHistoryDto> PriceHistory
);


public record ProductScrapedDto(
    string Title,
    decimal Price,
    string Currency,
    bool IsAvailable,
    string Url, // Necesitamos pasarla para saber de quién es el precio
    string Vendor // "Amazon", "MercadoLibre"
);
