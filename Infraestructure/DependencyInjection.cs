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

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Base de Datos (Postgres)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Repositorios
        services.AddScoped<IProductRepository, ProductRepository>();

        // 3. Scraping
        services.AddSingleton<PlaywrightEngine>(); // Motor pesado compartido
        services.AddTransient<ScraperFactory>(); // Fábrica ligera

        // Registramos las estrategias específicas
        services.AddTransient<IScraperStrategy, AmazonStrategy>();
        services.AddTransient<IScraperStrategy, MercadoLibreStrategy>();

        // Registramos el servicio que usa la Application
        services.AddScoped<IScraperService, ScraperService>();

        // Configuracion de hangfire
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }
    
    
    public static IApplicationBuilder UseHangfire(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard();
        
        return app;
    }
}
