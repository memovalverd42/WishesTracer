// DependencyInjection.cs - Infrastructure layer service registration
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using WishesTracer.Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WishesTracer.Application.Interfaces;
using WishesTracer.Domain.Interfaces;
using WishesTracer.Infraestructure.Persistence.Repositories;
using WishesTracer.Infraestructure.Scraper;
using WishesTracer.Infraestructure.Scraper.Core;
using WishesTracer.Infraestructure.Services;

namespace WishesTracer.Infraestructure;

/// <summary>
/// Provides extension methods for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure layer services in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The configured service collection for chaining</returns>
    /// <remarks>
    /// Configures:
    /// - PostgreSQL database context with EF Core
    /// - Redis distributed cache
    /// - Repository implementations (Scoped lifetime)
    /// - Web scraping services with Playwright (Singleton for engine, Transient for 
    ///   strategies)
    /// - Hangfire background job processing
    /// </remarks>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Base de Datos (Postgres)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        // Configuración de REDIS (Implementación de IDistributedCache)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "WishesTracer_"; 
        });

        // 2. Repositorios (Scoped: una instancia por request HTTP)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

        // 3. Scraping (Singleton para motor pesado, Transient para estrategias ligeras)
        services.AddSingleton<PlaywrightEngine>(); // Motor pesado compartido
        services.AddTransient<ScraperFactory>(); // Fábrica ligera

        // Registramos las estrategias específicas
        services.AddTransient<IScraperStrategy, AmazonStrategy>();
        services.AddTransient<IScraperStrategy, MercadoLibreStrategy>();

        // Registramos el servicio que usa la Application
        services.AddScoped<IScraperService, ScraperService>();

        // Configuracion de hangfire (Background job processing)
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }
    
    /// <summary>
    /// Configures Hangfire dashboard middleware in the application pipeline.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    /// <remarks>
    /// Enables the Hangfire dashboard at /hangfire for monitoring scheduled jobs.
    /// </remarks>
    public static IApplicationBuilder UseHangfire(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard();
        
        return app;
    }
}
