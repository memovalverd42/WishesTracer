using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WishesTracer.Shared.Results;

namespace WishesTracer.Shared.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Converts Result to IActionResult with ProblemDetails on failure
    /// Use for void operations (Result without value)
    /// </summary>
    public static IActionResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return CreateProblemDetailsResult(result.Error);
    }

    /// <summary>
    /// Converts Result&lt;T&gt; to ActionResult&lt;T&gt; with proper status codes
    /// Returns value on success, ProblemDetails on failure
    /// </summary>
    public static ActionResult<T> ToValueOrProblemDetails<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return CreateProblemDetailsResult(result.Error);
    }

    /// <summary>
    /// Converts Result&lt;T&gt; to ActionResult&lt;T&gt; with 201 Created status
    /// Use for POST operations that create resources
    /// </summary>
    public static ActionResult<T> ToCreatedAtRouteOrProblemDetails<T>(
        this Result<T> result,
        string routeName,
        Func<T, object> routeValuesSelector)
    {
        if (result.IsSuccess)
        {
            var routeValues = routeValuesSelector(result.Value);
            return new CreatedAtRouteResult(routeName, routeValues, result.Value);
        }

        return CreateProblemDetailsResult(result.Error);
    }

    /// <summary>
    /// Converts Result&lt;T&gt; to ActionResult&lt;T&gt; with 201 Created status
    /// Use for POST operations with location header
    /// </summary>
    public static ActionResult<T> ToCreatedOrProblemDetails<T>(
        this Result<T> result,
        string location)
    {
        if (result.IsSuccess)
        {
            return new CreatedResult(location, result.Value);
        }

        return CreateProblemDetailsResult(result.Error);
    }

    /// <summary>
    /// Converts Result to IActionResult with 204 No Content on success
    /// Use for PUT/PATCH operations
    /// </summary>
    public static IActionResult ToNoContentOrProblemDetails(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return CreateProblemDetailsResult(result.Error);
    }

    /// <summary>
    /// Converts Result&lt;T&gt; to ActionResult&lt;T&gt; with custom success status code
    /// </summary>
    public static ActionResult<T> ToValueOrProblemDetails<T>(
        this Result<T> result,
        int successStatusCode)
    {
        if (result.IsSuccess)
        {
            return new ObjectResult(result.Value)
            {
                StatusCode = successStatusCode
            };
        }

        return CreateProblemDetailsResult(result.Error);
    }

    /// <summary>
    /// Maps ErrorType to appropriate HTTP status code
    /// </summary>
    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// Maps ErrorType to RFC 7807 problem type URI
    /// </summary>
    private static string GetProblemType(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
        ErrorType.NotFound => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
        ErrorType.Conflict => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
        ErrorType.Unauthorized => "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
        ErrorType.Forbidden => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
        _ => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
    };

    /// <summary>
    /// Maps ErrorType to HTTP status title
    /// </summary>
    private static string GetTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "Bad Request",
        ErrorType.NotFound => "Not Found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        _ => "Internal Server Error"
    };

    /// <summary>
    /// Creates a ProblemDetails result from an Error
    /// </summary>
    private static ObjectResult CreateProblemDetailsResult(Error error)
    {
        var statusCode = GetStatusCode(error.Type);
        
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(error.Type),
            Detail = error.Description,
            Type = GetProblemType(error.Type)
        };

        // Add error code as extension
        problemDetails.Extensions["errorCode"] = error.Code;

        // Add timestamp
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        // Add metadata if present
        if (error.Metadata != null && error.Metadata.Any())
        {
            foreach (var (key, value) in error.Metadata)
            {
                problemDetails.Extensions[ToCamelCase(key)] = value;
            }
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Converts string to camelCase for JSON serialization
    /// </summary>
    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
