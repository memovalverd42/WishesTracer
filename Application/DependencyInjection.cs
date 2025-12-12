// DependencyInjection.cs - Application layer service registration
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WishesTracer.Application.Behaviors;

namespace WishesTracer.Application;

/// <summary>
/// Provides extension methods for registering Application layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Application layer services in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The configured service collection for chaining</returns>
    /// <remarks>
    /// Configures MediatR with automatic handler discovery and registers the caching
    /// pipeline behavior for queries implementing ICacheableQuery.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR escanea automáticamente este ensamblado (Assembly) buscando Handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        
        return services;
    }
}
