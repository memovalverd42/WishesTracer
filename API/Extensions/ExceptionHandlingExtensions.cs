using WishesTracer.Exceptions;

namespace WishesTracer.Extensions;

public static class ExceptionHandlingExtensions
{
    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            // Customize ProblemDetails generation
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["nodeId"] = Environment.MachineName;
            };
        });

        return services;
    }

    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        
        return app;
    }
}
