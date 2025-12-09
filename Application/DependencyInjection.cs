using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace WishesTracer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR escanea automáticamente este ensamblado (Assembly) buscando Handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Aquí registrarías Validadores o Mappers automáticos
        
        return services;
    }
}
