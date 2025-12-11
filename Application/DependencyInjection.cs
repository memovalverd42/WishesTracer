using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WishesTracer.Application.Behaviors;

namespace WishesTracer.Application;

public static class DependencyInjection
{
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
