using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WishesTracer.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Exception occurred: {Message}",
            exception.Message);

        var problemDetails = CreateProblemDetails(httpContext, exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var type = GetProblemType(exception);
        var title = GetTitle(exception);
        var detail = GetDetail(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Type = type,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        // Add correlation ID for tracking
        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        // Add trace ID
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        // Add timestamp
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        // Add exception details only in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            
            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    type = exception.InnerException.GetType().Name,
                    message = exception.InnerException.Message
                };
            }
        }

        // Add specific details based on exception type
        AddExceptionSpecificDetails(problemDetails, exception);

        return problemDetails;
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        // Validation exceptions
        ArgumentNullException => StatusCodes.Status400BadRequest,
        ArgumentException => StatusCodes.Status400BadRequest,
        InvalidOperationException => StatusCodes.Status400BadRequest,
        
        // Database exceptions
        DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
        DbUpdateException => StatusCodes.Status409Conflict,
        
        // Unauthorized/Forbidden
        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
        
        // Not found
        KeyNotFoundException => StatusCodes.Status404NotFound,
        
        // External service errors (Playwright, HTTP clients, etc.)
        HttpRequestException => StatusCodes.Status502BadGateway,
        TaskCanceledException => StatusCodes.Status504GatewayTimeout,
        TimeoutException => StatusCodes.Status504GatewayTimeout,
        
        // Default
        _ => StatusCodes.Status500InternalServerError
    };

    private static string GetProblemType(Exception exception) => exception switch
    {
        ArgumentNullException => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
        ArgumentException => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
        DbUpdateConcurrencyException => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
        DbUpdateException => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
        UnauthorizedAccessException => "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
        KeyNotFoundException => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
        HttpRequestException => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.3",
        TaskCanceledException => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.5",
        TimeoutException => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.5",
        _ => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
    };

    private static string GetTitle(Exception exception) => exception switch
    {
        ArgumentNullException => "Bad Request",
        ArgumentException => "Bad Request",
        InvalidOperationException => "Bad Request",
        DbUpdateConcurrencyException => "Conflict",
        DbUpdateException => "Conflict",
        UnauthorizedAccessException => "Unauthorized",
        KeyNotFoundException => "Not Found",
        HttpRequestException => "Bad Gateway",
        TaskCanceledException => "Gateway Timeout",
        TimeoutException => "Gateway Timeout",
        _ => "Internal Server Error"
    };

    private string GetDetail(Exception exception)
    {
        // In production, return generic messages for security
        if (!_environment.IsDevelopment())
        {
            return exception switch
            {
                ArgumentNullException or ArgumentException or InvalidOperationException 
                    => "The request contains invalid data.",
                DbUpdateConcurrencyException 
                    => "The record was modified by another user. Please refresh and try again.",
                DbUpdateException 
                    => "A database error occurred while processing your request.",
                UnauthorizedAccessException 
                    => "You are not authorized to perform this action.",
                KeyNotFoundException 
                    => "The requested resource was not found.",
                HttpRequestException or TaskCanceledException or TimeoutException 
                    => "An external service is currently unavailable. Please try again later.",
                _ => "An unexpected error occurred. Please try again later."
            };
        }

        // In development, return detailed messages
        return exception.Message;
    }

    private void AddExceptionSpecificDetails(ProblemDetails problemDetails, Exception exception)
    {
        switch (exception)
        {
            case DbUpdateConcurrencyException concurrencyEx:
                problemDetails.Extensions["affectedEntities"] = concurrencyEx.Entries
                    .Select(e => new
                    {
                        entityType = e.Entity.GetType().Name,
                        state = e.State.ToString()
                    })
                    .ToList();
                break;

            case DbUpdateException { InnerException: not null } dbEx:
                // Handle specific database constraint violations
                var innerMessage = dbEx.InnerException.Message.ToLower();
                
                if (innerMessage.Contains("unique constraint") || innerMessage.Contains("duplicate"))
                {
                    problemDetails.Title = "Conflict";
                    problemDetails.Detail = _environment.IsDevelopment() 
                        ? dbEx.InnerException.Message 
                        : "A record with this information already exists.";
                    problemDetails.Extensions["constraintViolation"] = "unique";
                }
                else if (innerMessage.Contains("foreign key"))
                {
                    problemDetails.Title = "Conflict";
                    problemDetails.Detail = _environment.IsDevelopment() 
                        ? dbEx.InnerException.Message 
                        : "This operation would violate data integrity constraints.";
                    problemDetails.Extensions["constraintViolation"] = "foreignKey";
                }
                break;

            case HttpRequestException httpEx:
                problemDetails.Extensions["statusCode"] = httpEx.StatusCode?.ToString();
                
                // If it's from Playwright or similar scraping tools
                if (httpEx.Source?.Contains("Playwright") == true || 
                    httpEx.Source?.Contains("PuppeteerSharp") == true)
                {
                    problemDetails.Extensions["serviceType"] = "web-scraper";
                    problemDetails.Detail = _environment.IsDevelopment()
                        ? httpEx.Message
                        : "Failed to retrieve data from the target website.";
                }
                break;

            case TaskCanceledException or TimeoutException:
                problemDetails.Extensions["timeoutType"] = exception is TaskCanceledException 
                    ? "task-cancelled" 
                    : "timeout";
                break;

            case ArgumentException argEx when !string.IsNullOrEmpty(argEx.ParamName):
                problemDetails.Extensions["parameterName"] = argEx.ParamName;
                break;
        }
    }
}
