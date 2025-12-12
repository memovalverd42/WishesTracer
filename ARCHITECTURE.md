# Arquitectura de WishesTracer

## Índice
- [Arquitectura General](#arquitectura-general)
- [Flujo Principal de la Aplicación](#flujo-principal-de-la-aplicación)
- [Operaciones CRUD](#operaciones-crud)
- [Flujo de Monitoreo de Precios](#flujo-de-monitoreo-de-precios)
- [Manejo de Errores y Middleware](#manejo-de-errores-y-middleware)
- [Dependency Injection](#dependency-injection)
- [Patrones de Diseño](#patrones-de-diseño)

## Arquitectura General

El proyecto sigue **Clean Architecture** con separación clara de responsabilidades en capas:

```mermaid
graph TB
    subgraph "API Layer"
        Controllers[Controllers]
        Middleware[Global Exception Handler]
        Program[Program.cs]
    end

    subgraph "Application Layer"
        Commands[Commands]
        Queries[Queries]
        Events[Event Handlers]
        Behaviors[Pipeline Behaviors]
        AppInterfaces[Application Interfaces]
    end

    subgraph "Domain Layer"
        Entities[Entities]
        DomainInterfaces[Repository Interfaces]
        DomainErrors[Domain Errors]
        DomainEvents[Domain Events]
    end

    subgraph "Infrastructure Layer"
        Repositories[Repositories]
        DbContext[EF Core DbContext]
        Scraper[Web Scraper]
        Services[Infrastructure Services]
    end

    subgraph "External Services"
        PostgreSQL[(PostgreSQL)]
        Redis[(Redis Cache)]
        Hangfire[Hangfire Background Jobs]
        Playwright[Playwright Browser]
    end

    Controllers --> Commands
    Controllers --> Queries
    Commands --> Entities
    Queries --> Entities
    Commands --> AppInterfaces
    Queries --> DomainInterfaces
    Events --> DomainEvents
    
    AppInterfaces -.implements.-> Services
    DomainInterfaces -.implements.-> Repositories
    
    Repositories --> DbContext
    DbContext --> PostgreSQL
    Services --> Scraper
    Scraper --> Playwright
    Behaviors --> Redis
    
    Program --> Hangfire
    Hangfire --> Commands

    style API fill:#e1f5ff
    style Application fill:#fff3e0
    style Domain fill:#f3e5f5
    style Infrastructure fill:#e8f5e9
    style External fill:#fce4ec
```

### Principios de Clean Architecture

1. **Independencia de frameworks**: El dominio no depende de Entity Framework, ASP.NET, etc.
2. **Testeable**: Las reglas de negocio pueden probarse sin UI, BD o servicios externos
3. **Independencia de UI**: La UI puede cambiar sin afectar el resto del sistema
4. **Independencia de BD**: Puedes cambiar de PostgreSQL a SQL Server sin tocar el dominio
5. **Independencia de agentes externos**: Las reglas de negocio no saben de scrapers o APIs

## Flujo Principal de la Aplicación

### Inicialización de la Aplicación

```mermaid
sequenceDiagram
    participant Main as Program.cs
    participant Builder as WebApplicationBuilder
    participant DI as Dependency Injection
    participant App as WebApplication
    participant Hangfire as Hangfire Manager

    Main->>Builder: CreateBuilder()
    Builder->>DI: AddApplication()
    Note over DI: Registra MediatR<br/>Pipeline Behaviors
    Builder->>DI: AddInfrastructure()
    Note over DI: Registra EF Core<br/>Redis, Hangfire<br/>Scrapers
    Builder->>Builder: AddSwaggerGen()
    Builder->>App: Build()
    
    App->>App: UseGlobalExceptionHandling()
    App->>App: UseSwagger()
    App->>App: MapControllers()
    App->>App: UseHangfire()
    
    Main->>Hangfire: AddOrUpdate("check-prices")
    Note over Hangfire: Programa job<br/>cada hora (Cron.Hourly)
    
    App->>App: Run()
```

## Operaciones CRUD

### Crear Producto (POST /api/products)

```mermaid
sequenceDiagram
    participant Client
    participant Controller as ProductsController
    participant Mediator as MediatR
    participant Handler as CreateProductHandler
    participant Scraper as ScraperService
    participant Playwright as PlaywrightEngine
    participant Repo as ProductRepository
    participant DB as PostgreSQL

    Client->>Controller: POST /api/products<br/>{url: "..."}
    Controller->>Mediator: Send(CreateProductCommand)
    Mediator->>Handler: Handle(command)
    
    Handler->>Handler: Validate URL
    Handler->>Repo: ExistsWithUrlAsync(cleanedUrl)
    Repo->>DB: SELECT
    DB-->>Repo: null (no existe)
    Repo-->>Handler: null
    
    Handler->>Scraper: ScrapeProductAsync(url)
    Scraper->>Playwright: GetHtmlContentAsync(url)
    Note over Scraper,Playwright: Rate limiting:<br/>Max 2 concurrent<br/>Random delay 2-5s
    Playwright-->>Scraper: HTML content
    Scraper->>Scraper: ParseHtml(strategy)
    Scraper-->>Handler: ProductScrapedDto
    
    Handler->>Handler: new Product(title, url, vendor)
    Handler->>Handler: product.UpdatePrice(...)
    Handler->>Repo: AddAsync(product)
    Repo->>DB: INSERT
    DB-->>Repo: Success
    
    Handler-->>Controller: Result<ProductDto>
    Controller->>Controller: ToValueOrProblemDetails()
    Controller-->>Client: 201 Created<br/>ProductDto
```

### Obtener Productos con Caché (GET /api/products)

```mermaid
sequenceDiagram
    participant Client
    participant Controller
    participant Mediator
    participant CacheBehavior as CachingBehavior
    participant Handler as GetProductsHandler
    participant Cache as Redis
    participant Repo as ProductRepository
    participant DB as PostgreSQL

    Client->>Controller: GET /api/products?page=1&pageSize=10
    Controller->>Mediator: Send(GetProductsQuery)
    Mediator->>CacheBehavior: Handle(query)
    
    CacheBehavior->>Cache: GetStringAsync(cacheKey)
    
    alt Cache Hit
        Cache-->>CacheBehavior: Cached data
        Note over CacheBehavior: 🚀 Cache HIT
        CacheBehavior->>CacheBehavior: Deserialize<PagedResult>
        CacheBehavior-->>Controller: Result<PagedResult>
    else Cache Miss
        Cache-->>CacheBehavior: null
        Note over CacheBehavior: 🐌 Cache MISS
        CacheBehavior->>Handler: next() - Execute handler
        Handler->>Repo: GetPagedAsync(page, pageSize)
        Repo->>DB: SELECT with pagination
        DB-->>Repo: Products
        Repo-->>Handler: (products, totalCount)
        Handler->>Handler: Map to DTOs
        Handler-->>CacheBehavior: Result<PagedResult>
        CacheBehavior->>CacheBehavior: Serialize result
        CacheBehavior->>Cache: SetStringAsync(key, data, 5min)
        Cache-->>CacheBehavior: OK
        CacheBehavior-->>Controller: Result<PagedResult>
    end
    
    Controller->>Controller: ToValueOrProblemDetails()
    Controller-->>Client: 200 OK<br/>PagedResult
```

## Flujo de Monitoreo de Precios

### Background Job de Hangfire (Ejecuta cada hora)

```mermaid
sequenceDiagram
    participant Hangfire
    participant Mediator
    participant Handler as CheckProductPricesHandler
    participant Repo as ProductRepository
    participant Scraper as ScraperService
    participant Publisher as MediatR Publisher
    participant EventHandler as PriceChangedEventHandler
    participant Cache as Redis

    Note over Hangfire: Cron.Hourly triggered
    Hangfire->>Mediator: Send(CheckProductPricesCommand)
    Mediator->>Handler: Handle(command)
    
    Handler->>Repo: GetActiveProductIdsAsync()
    Repo-->>Handler: List<Guid> activeIds
    
    loop For each product ID
        Handler->>Repo: GetByIdAsync(productId)
        Repo-->>Handler: Product
        
        alt Product is Active
            Handler->>Scraper: ScrapeProductAsync(url)
            Note over Scraper: Rate limiting +<br/>Random delay
            Scraper-->>Handler: ProductScrapedDto
            
            Handler->>Handler: previousPrice = product.CurrentPrice
            Handler->>Handler: product.UpdatePrice(newPrice, ...)
            
            Handler->>Repo: SaveChangesAsync()
            Repo-->>Handler: Success
            
            alt Price Changed
                Handler->>Publisher: Publish(PriceChangedEvent)
                Publisher->>EventHandler: Handle(event)
                EventHandler->>Cache: RemoveAsync("product-history:...")
                EventHandler->>Cache: RemoveAsync("product-details:...")
                Note over EventHandler: 🔔 Log price change<br/>alert (could send email)
                EventHandler-->>Publisher: Done
            else No Price Change
                Note over Handler: 📊 Log: no changes
            end
        else Product Inactive
            Note over Handler: ⏭️ Skip product
        end
    end
    
    Handler-->>Mediator: Task completed
```

### Invalidación de Caché en Cambios de Precio

```mermaid
graph LR
    A[Price Changed Event] --> B[PriceChangedEventHandler]
    B --> C{Invalidate Cache}
    C --> D[Remove product-history:id]
    C --> E[Remove product-details:id]
    C --> F[Log Alert]
    F --> G[Future: Send Email/SMS/WebSocket]
    
    style A fill:#fff3e0
    style B fill:#e1f5ff
    style C fill:#f3e5f5
    style G fill:#e8f5e9,stroke-dasharray: 5 5
```

## Manejo de Errores y Middleware

### Pipeline de Middleware

```mermaid
graph TB
    Request[HTTP Request] --> ExceptionHandler[Global Exception Handler]
    ExceptionHandler --> Routing[Routing]
    Routing --> Authorization[Authorization]
    Authorization --> Controller[Controller]
    Controller --> MediatR
    
    subgraph "MediatR Pipeline"
        MediatR --> CachingBehavior
        CachingBehavior --> Handler[Command/Query Handler]
        Handler --> Domain[Domain Logic]
    end
    
    Domain --> Response[Response]
    
    ExceptionHandler -.catches.-> Exception[Unhandled Exception]
    Exception --> ProblemDetails[RFC 7807 Problem Details]
    ProblemDetails --> ErrorResponse[Error Response]
    
    Response --> Client[HTTP Response]
    ErrorResponse --> Client

    style ExceptionHandler fill:#ffebee
    style ProblemDetails fill:#fff3e0
    style Domain fill:#f3e5f5
```

### Result Pattern y Error Handling

```mermaid
graph TB
    Operation[Domain Operation] --> Result{Result Pattern}
    
    Result -->|Success| SuccessPath[Result.Success&lt;T&gt;]
    Result -->|Failure| FailurePath[Result.Failure&lt;T&gt;]
    
    SuccessPath --> Controller[Controller]
    FailurePath --> ErrorType{Error Type}
    
    ErrorType -->|Validation| V[400 Bad Request]
    ErrorType -->|NotFound| NF[404 Not Found]
    ErrorType -->|Conflict| C[409 Conflict]
    ErrorType -->|Unauthorized| U[401 Unauthorized]
    ErrorType -->|Forbidden| F[403 Forbidden]
    ErrorType -->|Failure| S[500 Internal Server Error]
    
    V --> PD[Problem Details]
    NF --> PD
    C --> PD
    U --> PD
    F --> PD
    S --> PD
    
    Controller --> ToValue[ToValueOrProblemDetails]
    PD --> ToValue
    ToValue --> Response[HTTP Response]

    style Result fill:#e1f5ff
    style ErrorType fill:#ffebee
    style PD fill:#fff3e0
```

## Dependency Injection

### Configuración de Servicios por Lifetime

```mermaid
graph TB
    subgraph "Singleton Lifetime (Shared)"
        S1[PlaywrightEngine]
        S2[ILogger]
    end
    
    subgraph "Scoped Lifetime (Per Request)"
        Sc1[DbContext]
        Sc2[IProductRepository]
        Sc3[IScraperService]
        Sc4[IMediator Request Handlers]
    end
    
    subgraph "Transient Lifetime (Per Use)"
        T1[IScraperStrategy instances]
        T2[ScraperFactory]
    end
    
    Request[HTTP Request] --> Scope[Create Scope]
    Scope --> Sc1
    Scope --> Sc2
    Scope --> Sc3
    Scope --> Sc4
    
    Sc3 --> S1
    Sc3 --> T2
    T2 --> T1
    
    Sc2 --> Sc1
    Sc4 --> Sc2

    style Singleton fill:#e3f2fd
    style Scoped fill:#f3e5f5
    style Transient fill:#fff3e0
```

### Registro de Servicios

```mermaid
sequenceDiagram
    participant Program
    participant AppDI as Application DI
    participant InfraDI as Infrastructure DI
    
    Program->>Program: CreateBuilder()
    
    Program->>AppDI: AddApplication()
    Note over AppDI: services.AddMediatR()<br/>RegisterHandlers<br/>AddOpenBehavior(CachingBehavior)
    
    Program->>InfraDI: AddInfrastructure(config)
    Note over InfraDI: AddDbContext<ApplicationDbContext><br/>AddStackExchangeRedisCache<br/>AddScoped<IProductRepository><br/>AddSingleton<PlaywrightEngine><br/>AddTransient<IScraperStrategy><br/>AddHangfire
    
    Program->>Program: Build()
```

## Patrones de Diseño

### 1. CQRS (Command Query Responsibility Segregation)

```mermaid
graph LR
    Client --> API
    
    API --> Commands[Commands - Write]
    API --> Queries[Queries - Read]
    
    Commands --> WriteModel[Write Model]
    Queries --> ReadModel[Read Model + Cache]
    
    WriteModel --> DB[(Database)]
    ReadModel --> Cache[(Redis Cache)]
    ReadModel -.fallback.-> DB

    style Commands fill:#ffebee
    style Queries fill:#e3f2fd
```

### 2. Repository Pattern

```
IProductRepository (Domain)
        ↓ implements
ProductRepository (Infrastructure)
        ↓ uses
ApplicationDbContext (EF Core)
```

### 3. Strategy Pattern (Web Scraping)

```mermaid
graph TB
    ScraperService --> Factory[ScraperFactory]
    Factory --> Strategy{IScraperStrategy}
    Strategy --> Amazon[AmazonScraperStrategy]
    Strategy --> ML[MLScraperStrategy]
    
    Amazon --> Parse1[ParseHtml for Amazon]
    ML --> Parse2[ParseHtml for MercadoLibre]
    
    Parse1 --> DTO[ProductScrapedDto]
    Parse2 --> DTO

    style Factory fill:#fff3e0
    style Strategy fill:#e1f5ff
```

### 4. Mediator Pattern (MediatR)

```mermaid
graph TB
    Controller --> Mediator[MediatR]
    
    Mediator --> Pipeline[Pipeline Behaviors]
    Pipeline --> Cache[CachingBehavior]
    
    Cache --> Handlers{Handlers}
    Handlers --> Command[Command Handlers]
    Handlers --> Query[Query Handlers]
    
    Command --> Domain[Domain Logic]
    Query --> Repository[Repository]

    style Mediator fill:#e1f5ff
    style Pipeline fill:#fff3e0
```

### 5. Result Pattern (Railway-Oriented Programming)

```mermaid
graph LR
    A[Operation Start] --> B{Validation}
    B -->|Valid| C[Business Logic]
    B -->|Invalid| E1[Error: Validation]
    
    C --> D{Success?}
    D -->|Yes| S[Result.Success<T>]
    D -->|No| E2[Error: Business Rule]
    
    E1 --> F[Result.Failure<T>]
    E2 --> F
    
    S --> Response
    F --> Response

    style S fill:#c8e6c9
    style F fill:#ffcdd2
```

## Consideraciones de Seguridad

1. **Rate Limiting en Scraper**: Máximo 2 requests concurrentes, delays aleatorios
2. **SQL Injection**: Protegido por Entity Framework parametrizado
3. **Nullable Reference Types**: Habilitados en todos los proyectos
4. **Input Validation**: Data annotations + FluentValidation
5. **Exception Handling**: Global handler evita leaking de información sensible
6. **Connection Strings**: Nunca en código, solo en appsettings o secrets

## Escalabilidad

### Estrategias Implementadas

1. **Distributed Caching**: Redis para queries frecuentes
2. **Pagination**: Evita cargar grandes datasets en memoria
3. **Background Jobs**: Hangfire desacopla el procesamiento pesado
4. **Async/Await**: Operaciones I/O no bloqueantes
5. **Strategy Pattern**: Fácil agregar nuevos vendors sin modificar código existente

### Futuras Mejoras

- [ ] API Rate Limiting con AspNetCoreRateLimit
- [ ] Health checks con /health endpoint
- [ ] Outbox pattern para garantía de eventos
- [ ] Read replicas de PostgreSQL
- [ ] Horizontal scaling con Redis Cluster
