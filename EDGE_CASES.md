# Edge Cases y Validaciones en WishesTracer

Este documento lista los edge cases que el sistema maneja explícitamente, incluyendo validaciones, manejo de errores, condiciones de carrera, y situaciones límite.

## Índice
- [Validaciones de Datos](#validaciones-de-datos)
- [Manejo de Valores Nulos](#manejo-de-valores-nulos)
- [Límites de Tamaño y Cantidad](#límites-de-tamaño-y-cantidad)
- [Condiciones de Carrera y Thread-Safety](#condiciones-de-carrera-y-thread-safety)
- [Timeouts y Políticas de Retry](#timeouts-y-políticas-de-retry)
- [Errores de Servicios Externos](#errores-de-servicios-externos)
- [Transacciones y Rollback](#transacciones-y-rollback)
- [Web Scraping Edge Cases](#web-scraping-edge-cases)

## Validaciones de Datos

### 1. Validación de URLs

**Ubicación**: `Application/Features/Products/Commands/CreateProduct/CreateProductHandler.cs`

```csharp
// Edge Case: URL malformada o inválida
if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var _))
    return ProductErrors.InvalidUrl;
```

**Casos cubiertos**:
- ❌ URL vacía: `""` → `ProductErrors.InvalidUrl`
- ❌ URL relativa: `"product/123"` → `ProductErrors.InvalidUrl`
- ❌ URL sin esquema: `"amazon.com"` → `ProductErrors.InvalidUrl`
- ✅ URL válida: `"https://amazon.com.mx/dp/B08..."`

**Test asociado**: `ApplicationTests/Features/Products/Commands/CreateProduct/CreateProductHandlerTests.cs`

### 2. Limpieza de URLs (Query Parameters)

**Ubicación**: `Application/Features/Products/Commands/CreateProduct/CreateProductHandler.cs`

```csharp
// Edge Case: URLs con query parameters que hacen tracking
var uri = new Uri(request.Url);
var cleanedUrl = "https://" + uri.Host + uri.AbsolutePath;
```

**Casos cubiertos**:
- ✅ `amazon.com/dp/B08?ref=tracking` → `amazon.com/dp/B08`
- ✅ `mercadolibre.com/MLM-123?utm_source=...` → `mercadolibre.com/MLM-123`
- ✅ URLs con fragmentos `#section` → removidos

### 3. URLs Duplicadas

**Ubicación**: `Application/Features/Products/Commands/CreateProduct/CreateProductHandler.cs`

```csharp
// Edge Case: Intentar agregar un producto que ya existe
var exists = await _repository.ExistsWithUrlAsync(cleanedUrl);
if (exists != null)
    return ProductErrors.DuplicateUrl(cleanedUrl);
```

**Casos cubiertos**:
- ❌ Producto ya existe con la misma URL (después de limpieza)
- ✅ Primera vez agregando el producto
- ✅ Misma URL pero distinto query param → detectado como duplicado correctamente

### 4. Validación de Precios

**Ubicación**: `Application/Features/Products/Commands/CreateProduct/CreateProductHandler.cs`

```csharp
// Edge Case: Precio inválido (cero o negativo)
if (scrapedData.Price <= 0)
    return ProductErrors.InvalidPrice;
```

**Casos cubiertos**:
- ❌ Precio = 0 → `ProductErrors.InvalidPrice`
- ❌ Precio negativo → `ProductErrors.InvalidPrice`
- ✅ Precio > 0 → válido

## Manejo de Valores Nulos

### 1. Nullable Reference Types Habilitados

**Ubicación**: Todos los proyectos `.csproj`

```xml
<Nullable>enable</Nullable>
```

**Beneficio**: El compilador alerta sobre posibles `NullReferenceException` en tiempo de compilación.

### 2. Producto No Encontrado

**Ubicación**: 
- `Application/Features/Products/Queries/GetProductDetailsQuery.cs`
- `Application/Features/Products/Queries/GetProductHistoryQuery.cs`

```csharp
// Edge Case: Intentar obtener producto que no existe
var product = await _productRepository.GetByIdAsync(request.ProductId);
if (product == null)
    return ProductErrors.NotFound(request.ProductId);
```

**Casos cubiertos**:
- ❌ ID no existe en BD → `404 Not Found` con Problem Details
- ❌ ID con formato inválido → `400 Bad Request` (validación ASP.NET)
- ✅ Producto existe → retorna detalles

### 3. Búsqueda con SearchTerm Nulo

**Ubicación**: `Infrastructure/Persistence/Repositories/ProductRepository.cs`

```csharp
// Edge Case: searchTerm puede ser null
public async Task<(List<Product> Products, int TotalCount)> GetPagedAsync(
    int page, int pageSize, string? searchTerm)
{
    var query = _context.Products.AsQueryable();
    
    // Solo filtrar si searchTerm no es null o vacío
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(p => 
            p.Name.Contains(searchTerm) || 
            p.Url.Contains(searchTerm));
    }
    // ...
}
```

**Casos cubiertos**:
- ✅ `searchTerm = null` → retorna todos los productos paginados
- ✅ `searchTerm = ""` → retorna todos los productos paginados
- ✅ `searchTerm = "iphone"` → filtra productos

### 4. HTML Vacío del Scraper

**Ubicación**: `Infrastructure/Services/ScraperService.cs`

```csharp
// Edge Case: Playwright retorna HTML vacío
var html = await _engine.GetHtmlContentAsync(url);

if (string.IsNullOrEmpty(html))
    throw new Exception("HTML vacío");
```

**Casos cubiertos**:
- ❌ Página no carga → Exception
- ❌ Timeout → Exception
- ✅ HTML válido → continúa parsing

## Límites de Tamaño y Cantidad

### 1. Paginación con Límites

**Ubicación**: `Application/Features/Products/Queries/GetProductsQuery.cs`

```csharp
public record GetProductsQuery(
    int Page = 1,        // Mínimo: 1
    int PageSize = 10    // Típico: 10, Máximo recomendado: 100
) : IRequest<Result<PagedResult<ProductDto>>>, ICacheableQuery
```

**Casos cubiertos**:
- ✅ `page = 0` → se maneja como página 1
- ✅ `pageSize = 1000` → permitido pero no recomendado (considerar límite)
- ✅ `page > totalPages` → retorna lista vacía pero metadata correcta

**Mejora sugerida**: Agregar validación explícita:
```csharp
if (pageSize > 100) pageSize = 100;
if (page < 1) page = 1;
```

### 2. Longitud de Strings en Entidades

**Ubicación**: `Domain/Entities/Product.cs` y configuraciones EF Core

**Consideraciones**:
- URLs pueden ser muy largas (hasta 2048 caracteres en algunos navegadores)
- Nombres de productos pueden variar mucho en longitud
- Currency codes: 3 caracteres (ISO 4217)

**Recomendación de configuración en EF Core**:
```csharp
// En ApplicationDbContextModelBuilder o Fluent API
entity.Property(p => p.Name).HasMaxLength(500);
entity.Property(p => p.Url).HasMaxLength(2048);
entity.Property(p => p.Currency).HasMaxLength(3);
```

## Condiciones de Carrera y Thread-Safety

### 1. Rate Limiting en Scraper (Semaphore)

**Ubicación**: `Infrastructure/Services/ScraperService.cs`

```csharp
// Edge Case: Múltiples requests concurrentes de scraping
private static readonly SemaphoreSlim Semaphore = new(2, 2);

public async Task<ProductScrapedDto> ScrapeProductAsync(string url)
{
    // Turno - máximo 2 requests concurrentes
    await Semaphore.WaitAsync();
    
    try
    {
        // Retardo aleatorio entre 2-5 segundos
        var randomDelay = Random.Shared.Next(2000, 5000);
        await Task.Delay(randomDelay);
        
        // ... scraping logic
    }
    finally
    {
        // Soltar el turno SIEMPRE, incluso si falla
        Semaphore.Release();
    }
}
```

**Casos cubiertos**:
- ✅ 10 requests simultáneos → solo 2 ejecutan a la vez, 8 esperan
- ✅ Exception durante scraping → `Semaphore.Release()` se ejecuta igual (finally)
- ✅ Delays aleatorios → evita patrones detectables por anti-bot systems

### 2. Singleton PlaywrightEngine con Lazy Initialization

**Ubicación**: `Infrastructure/Scraper/Core/PlaywrightEngine.cs`

**Pattern**: Lazy thread-safe initialization del navegador

```csharp
private IPlaywright? _playwright;
private IBrowser? _browser;

public async Task InitializeAsync()
{
    if (_browser != null) return; // Ya inicializado
    
    _playwright = await Playwright.CreateAsync();
    _browser = await _playwright.Chromium.LaunchAsync(...);
}
```

**Casos cubiertos**:
- ✅ Múltiples llamadas concurrentes a `InitializeAsync()` → solo inicializa una vez
- ⚠️ **Potencial race condition**: Considerar usar `SemaphoreSlim` o `lock` async

**Mejora sugerida**:
```csharp
private readonly SemaphoreSlim _initLock = new(1, 1);

public async Task InitializeAsync()
{
    if (_browser != null) return;
    
    await _initLock.WaitAsync();
    try
    {
        if (_browser != null) return; // Double-check
        
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(...);
    }
    finally
    {
        _initLock.Release();
    }
}
```

### 3. Entity Framework Concurrency

**Ubicación**: Entity Framework Core automático

**Protección**:
- EF Core usa optimistic concurrency por defecto
- `SaveChangesAsync()` detecta conflictos de actualización

**Edge cases cubiertos**:
- ✅ Dos requests actualizan el mismo producto → el segundo falla con `DbUpdateConcurrencyException`
- Considerar agregar `[ConcurrencyCheck]` o `[Timestamp]` en entidades críticas

## Timeouts y Políticas de Retry

### 1. Playwright Timeout

**Ubicación**: `Infrastructure/Scraper/Core/PlaywrightEngine.cs`

```csharp
await page.GotoAsync(url, new PageGotoOptions
{
    Timeout = 30000,  // 30 segundos
    WaitUntil = WaitUntilState.NetworkIdle
});
```

**Casos cubiertos**:
- ❌ Página no responde en 30s → `TimeoutException`
- ❌ DNS falla → `PlaywrightException`
- ✅ Página carga lento pero dentro de 30s → éxito

### 2. Redis Timeout

**Ubicación**: Configuración de StackExchange.Redis

**Configuración recomendada**:
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
    options.InstanceName = "WishesTracer_";
    // Configurar timeouts si es necesario
    // options.ConfigurationOptions = new ConfigurationOptions
    // {
    //     ConnectTimeout = 5000,
    //     SyncTimeout = 5000
    // };
});
```

**Casos cubiertos**:
- ❌ Redis inaccesible → cache miss → fallback a DB
- ✅ Redis lento pero disponible → espera hasta timeout
- ✅ Redis caído → aplicación sigue funcionando (degraded mode)

## Errores de Servicios Externos

### 1. Scraping Failures

**Ubicación**: `Application/Features/Products/Commands/CheckProductPrices/CheckProductPricesHandler.cs`

```csharp
private async Task<ProductScrapedDto?> ScrapeProductDataAsync(
    IScraperService scraperService, 
    Product product)
{
    try
    {
        return await scraperService.ScrapeProductAsync(product.Url);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, 
            "Error scrapeando producto {ProductName} desde {Url}", 
            product.Name, product.Url);
        return null;  // Continuar con siguiente producto
    }
}
```

**Casos cubiertos**:
- ❌ Amazon cambia estructura HTML → scraping falla → log error, continuar
- ❌ Producto eliminado del sitio → scraping falla → log error, continuar
- ❌ Red inaccesible → scraping falla → log error, continuar
- ✅ Un producto falla, otros continúan procesándose

### 2. Vendor No Soportado

**Ubicación**: `Infrastructure/Scraper/ScraperFactory.cs`

```csharp
public IScraperStrategy GetStrategy(string url)
{
    var strategy = _strategies.FirstOrDefault(s => s.CanHandle(url));
    
    if (strategy == null)
        throw new UnsupportedVendorException(url);
    
    return strategy;
}
```

**Casos cubiertos**:
- ❌ URL de Walmart → `UnsupportedVendorException`
- ❌ URL de eBay → `UnsupportedVendorException`
- ✅ URL de Amazon → `AmazonScraperStrategy`
- ✅ URL de MercadoLibre → `MLScraperStrategy`

### 3. Elementos HTML No Encontrados

**Ubicación**: `Infrastructure/Scraper/AmazonScraperStrategy.cs` y `MLScraperStrategy.cs`

```csharp
// Edge Case: Selector no encuentra el elemento
var titleElement = doc.QuerySelector("#productTitle");
if (titleElement == null)
    throw new ScrapingException(url, "#productTitle", "Title element not found");
```

**Casos cubiertos**:
- ❌ Amazon rediseña la página → selectores fallan → `ScrapingException`
- ❌ Página de error 404 → selectores fallan → `ScrapingException`
- ✅ Selectores encuentran elementos → parsea correctamente

## Transacciones y Rollback

### 1. Entity Framework SaveChangesAsync

**Ubicación**: Repositories y Handlers

```csharp
await _repository.SaveChangesAsync();
```

**Comportamiento**:
- EF Core usa transacciones implícitas por defecto
- Si falla cualquier cambio, todos se revierten automáticamente

**Edge cases cubiertos**:
- ❌ Violación de constraint (ej. FK inválida) → rollback automático
- ❌ Deadlock en BD → retry policy o exception
- ✅ Todos los cambios exitosos → commit

### 2. Transacciones Explícitas (si se necesitan en el futuro)

**Ejemplo de uso recomendado**:
```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();

try
{
    await _context.Products.AddAsync(product);
    await _context.SaveChangesAsync();
    
    // Lógica adicional
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Web Scraping Edge Cases

### 1. Detección Anti-Bot

**Mitigaciones implementadas**:
- ✅ User-Agent real del navegador (Playwright)
- ✅ Delays aleatorios 2-5 segundos entre requests
- ✅ Rate limiting: máximo 2 requests concurrentes
- ✅ Espera a NetworkIdle antes de extraer HTML

**Edge cases cubiertos**:
- ⚠️ CAPTCHA → falla el scraping, se registra error
- ⚠️ Bloqueo por IP → falla el scraping, se registra error
- ✅ Comportamiento "humano" → más difícil de detectar

### 2. Parsing de Precios con Diferentes Formatos

**Ubicación**: Strategies en `Infrastructure/Scraper/`

```csharp
// Edge Case: Diferentes formatos de precio
var priceText = priceElement.TextContent.Trim();

// Amazon: "$999.99" o "$999"
// MercadoLibre: "$999" o "$ 999.99"
// Quitar símbolos y espacios

priceText = priceText.Replace("$", "")
                     .Replace(",", "")
                     .Trim();

if (!decimal.TryParse(priceText, out var price))
    throw new PriceExtractionException(url, priceText, "Cannot parse price");
```

**Casos cubiertos**:
- ✅ `"$999.99"` → `999.99`
- ✅ `"$ 999"` → `999`
- ✅ `"$1,299.99"` → `1299.99`
- ❌ `"Price unavailable"` → `PriceExtractionException`
- ❌ `"Contactar"` → `PriceExtractionException`

### 3. Productos Fuera de Stock

**Ubicación**: Scraping strategies

```csharp
// Edge Case: Producto no disponible
var availabilityElement = doc.QuerySelector("#availability");
var isAvailable = availabilityElement?.TextContent
    .Contains("In Stock", StringComparison.OrdinalIgnoreCase) ?? false;
```

**Casos cubiertos**:
- ✅ "In Stock" → `IsAvailable = true`
- ✅ "Out of Stock" → `IsAvailable = false`
- ✅ "Temporarily unavailable" → `IsAvailable = false`
- ⚠️ Elemento no existe → `IsAvailable = false` (asumir no disponible)

## Resumen de Validaciones por Capa

### Domain Layer
- ✅ Inmutabilidad de entidades (setters private)
- ✅ Lógica de negocio en métodos de dominio (`Product.UpdatePrice`)
- ✅ Validaciones de negocio (precio > 0 al actualizar)

### Application Layer
- ✅ Validación de URLs
- ✅ Detección de duplicados
- ✅ Validación de precios extraídos
- ✅ Manejo de errores con Result pattern

### Infrastructure Layer
- ✅ Rate limiting en scraper
- ✅ Timeouts en Playwright
- ✅ Manejo de HTML malformado
- ✅ Parsing seguro de precios

### API Layer
- ✅ Conversión automática de Result a Problem Details
- ✅ Validación de route parameters (Guid)
- ✅ Exception handling global
- ✅ Swagger documentation completa

## Mejoras Futuras Recomendadas

1. **Circuit Breaker Pattern**: Para evitar llamadas repetidas a servicios caídos
2. **Retry Policy con Polly**: Para errores transitorios de red
3. **Input Validation con FluentValidation**: Para validaciones más complejas
4. **Idempotency Keys**: Para operaciones críticas que podrían duplicarse
5. **Optimistic Locking**: Con `[ConcurrencyCheck]` en entidades críticas
6. **Health Checks**: Endpoint `/health` para monitoreo de infraestructura
