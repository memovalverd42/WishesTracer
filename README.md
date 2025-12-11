# WishesTracer 🛍️📊

Sistema de monitoreo automático de precios para productos de comercio electrónico construido con .NET 8, siguiendo los principios de Clean Architecture y Domain-Driven Design.

## 📋 Descripción

WishesTracer es una API REST que permite rastrear y monitorear cambios de precios en productos de diferentes plataformas de e-commerce (Amazon, MercadoLibre). El sistema utiliza web scraping con Playwright para extraer precios de forma automatizada cada hora mediante background jobs con Hangfire.

## ✨ Características Principales

- 🔍 **Web Scraping Inteligente**: Extracción de precios utilizando Playwright con rate limiting y delays aleatorios
- 📈 **Historial de Precios**: Almacenamiento y visualización del histórico completo de cambios de precio
- ⚡ **Background Jobs**: Monitoreo automático cada hora con Hangfire
- 🎯 **Pattern Matching**: Strategy pattern para soportar múltiples vendors (Amazon, MercadoLibre)
- 🚀 **Caché Distribuido**: Redis para optimizar consultas frecuentes
- 📊 **Paginación Eficiente**: Listados paginados con filtrado por nombre/URL
- 🔔 **Notificaciones**: Sistema de eventos para alertas de cambios de precio
- 📝 **Documentación Swagger**: OpenAPI con XML documentation completo
- 🏗️ **Clean Architecture**: Separación clara de capas (Domain, Application, Infrastructure, API)
- 🧪 **Testing Completo**: Tests unitarios con xUnit, NSubstitute y FluentAssertions

## 🛠️ Stack Tecnológico

- **.NET 8** - Framework principal
- **ASP.NET Core Web API** - REST API
- **Entity Framework Core** - ORM
- **PostgreSQL** - Base de datos relacional
- **Redis** - Caché distribuido
- **Hangfire** - Background job processing
- **Playwright** - Web scraping headless browser
- **MediatR** - CQRS y mediator pattern
- **Serilog** - Structured logging
- **xUnit** - Testing framework
- **Docker Compose** - Containerización

## 📋 Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para PostgreSQL y Redis)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) o [JetBrains Rider](https://www.jetbrains.com/rider/) (opcional)

## 🚀 Instalación

### 1. Clonar el repositorio

```bash
git clone https://github.com/memovalverd42/WishesTracer.git
cd WishesTracer
```

### 2. Iniciar servicios de infraestructura

```bash
docker-compose up -d
```

Esto iniciará:
- **PostgreSQL** en el puerto `5433` (evita conflictos con instalaciones locales)
- **Redis** en el puerto `6378`

### 3. Configurar cadenas de conexión

Crear `appsettings.Development.json` en el proyecto `API`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=wishes_tracer;Username=wt_admin;Password=wt_@dm1n",
    "Redis": "localhost:6378"
  }
}
```

### 4. Aplicar migraciones de base de datos

```bash
cd Infraestructure
dotnet ef database update --startup-project ../API
```

O desde Visual Studio Package Manager Console:

```powershell
Update-Database -Project Infraestructure -StartupProject API
```

### 5. Instalar Playwright browsers

```bash
cd API
pwsh bin/Debug/net8.0/playwright.ps1 install
```

### 6. Ejecutar la aplicación

```bash
dotnet run --project API
```

La API estará disponible en:
- **HTTPS**: https://localhost:7122
- **HTTP**: http://localhost:5122
- **Swagger UI**: https://localhost:7122/swagger
- **Hangfire Dashboard**: https://localhost:7122/hangfire

## 📁 Estructura del Proyecto

```
WishesTracer/
├── API/                          # Capa de presentación (Controllers, Middleware)
│   ├── Controllers/              # API Controllers
│   ├── Exceptions/               # Exception handlers globales
│   ├── Extensions/               # Extension methods
│   └── Program.cs                # Application entry point
├── Application/                  # Capa de aplicación (CQRS, DTOs)
│   ├── Behaviors/                # MediatR pipeline behaviors (Caching)
│   ├── DTOs/                     # Data Transfer Objects
│   ├── Features/                 # Vertical slice organization
│   │   └── Products/
│   │       ├── Commands/         # Command handlers (CreateProduct, CheckPrices)
│   │       ├── Events/           # Event handlers (PriceChangedEvent)
│   │       └── Queries/          # Query handlers (GetProducts, GetHistory)
│   └── Interfaces/               # Application layer interfaces
├── Domain/                       # Capa de dominio (Entities, Business Logic)
│   ├── Entities/                 # Domain entities (Product, PriceHistory)
│   ├── Errors/                   # Domain errors
│   ├── Events/                   # Domain events
│   └── Interfaces/               # Repository interfaces
├── Infrastructure/               # Capa de infraestructura (DB, External Services)
│   ├── Persistence/              # Entity Framework (DbContext, Repositories)
│   ├── Scraper/                  # Web scraping strategies
│   │   ├── Core/                 # Playwright engine
│   │   ├── AmazonScraperStrategy.cs
│   │   └── MLScraperStrategy.cs
│   └── Services/                 # Infrastructure services
├── Shared/                       # Código compartido entre capas
│   ├── DTOs/                     # Shared DTOs (PagedResult)
│   ├── Results/                  # Result pattern implementation
│   └── Extensions/               # Extension methods
├── Tests/                        # Tests unitarios
│   ├── ApplicationTests/
│   ├── DomainTests/
│   └── InfrastructureTests/
└── compose.yaml                  # Docker Compose configuration
```

## 🔌 Endpoints de la API

### Productos

#### Crear producto para tracking
```http
POST /api/products
Content-Type: application/json

{
  "url": "https://www.amazon.com.mx/dp/B0XXXXXX"
}
```

**Respuesta 201 Created:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Producto Ejemplo",
  "price": 999.99,
  "currency": "MXN",
  "isActive": true
}
```

#### Obtener detalles de un producto
```http
GET /api/products/{id}
```

**Respuesta 200 OK:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Producto Ejemplo",
  "url": "https://www.amazon.com.mx/dp/B0XXXXXX",
  "vendor": "Amazon",
  "currentPrice": 999.99,
  "currency": "MXN",
  "isAvailable": true,
  "isActive": true,
  "lastChecked": "2024-01-15T10:30:00Z",
  "createdAt": "2024-01-01T08:00:00Z",
  "priceHistory": [
    {
      "price": 1099.99,
      "timestamp": "2024-01-01T08:00:00Z"
    },
    {
      "price": 999.99,
      "timestamp": "2024-01-15T10:00:00Z"
    }
  ]
}
```

#### Listar productos con paginación
```http
GET /api/products?page=1&pageSize=10&searchTerm=iphone
```

**Respuesta 200 OK:**
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 45,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Obtener historial de precios
```http
GET /api/products/{id}/history
```

**Respuesta 200 OK:**
```json
[
  {
    "price": 1099.99,
    "timestamp": "2024-01-01T08:00:00Z"
  },
  {
    "price": 999.99,
    "timestamp": "2024-01-15T10:00:00Z"
  }
]
```

### Manejo de Errores (RFC 7807 Problem Details)

Todos los errores siguen el estándar RFC 7807:

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "A product with URL 'https://...' already exists",
  "errorCode": "Product.DuplicateUrl",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## 🗄️ Migraciones de Entity Framework

### Crear una nueva migración
```bash
dotnet ef migrations add NombreMigracion --startup-project API --project Infraestructure
```

### Aplicar migraciones
```bash
dotnet ef database update --startup-project API --project Infraestructure
```

### Revertir última migración
```bash
dotnet ef database update PreviousMigrationName --startup-project API --project Infraestructure
```

## 🧪 Testing

Ejecutar todos los tests:
```bash
dotnet test
```

Ejecutar tests de un proyecto específico:
```bash
dotnet test DomainTests
dotnet test ApplicationTests
dotnet test InfrastructureTests
```

Con cobertura:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 🐳 Docker

### Desarrollo con Docker Compose
```bash
# Iniciar servicios
docker-compose up -d

# Ver logs
docker-compose logs -f

# Detener servicios
docker-compose down

# Limpiar volúmenes
docker-compose down -v
```

## ⚙️ Configuración

### Variables de Entorno

Las siguientes variables pueden configurarse en `appsettings.json` o variables de entorno:

- `ConnectionStrings__DefaultConnection` - Cadena de conexión PostgreSQL
- `ConnectionStrings__Redis` - Cadena de conexión Redis
- `Logging__LogLevel__Default` - Nivel de logging (Information, Warning, Error)

### Configuración de Hangfire

El job de monitoreo de precios se ejecuta cada hora. Para cambiar la frecuencia, modifica en `Program.cs`:

```csharp
recurringJobManager.AddOrUpdate<IMediator>(
    "check-prices", 
    mediator => mediator.Send(new CheckProductPricesCommand(), CancellationToken.None), 
    Cron.Hourly  // Cambiar a Cron.Minutely para pruebas
);
```

## 🤝 Contribución

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📝 Licencia

Este proyecto está bajo la licencia MIT. Ver el archivo `LICENSE` para más detalles.

## 👥 Autores

- **Memo Valverde** - [@memovalverd42](https://github.com/memovalverd42)

## 🙏 Agradecimientos

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) por Robert C. Martin
- [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/) para el patrón Result
- [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/) por Jimmy Bogard
