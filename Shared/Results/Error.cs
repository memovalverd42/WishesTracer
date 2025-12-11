// Error.cs - Error representation for Result pattern
using System.Text.Json.Serialization;

namespace WishesTracer.Shared.Results;

/// <summary>
/// Defines the types of errors that can occur in the application.
/// </summary>
public enum ErrorType
{
    /// <summary>Validation error (e.g., invalid input)</summary>
    Validation,
    
    /// <summary>Resource not found error</summary>
    NotFound,
    
    /// <summary>Conflict error (e.g., duplicate resource)</summary>
    Conflict,
    
    /// <summary>General failure error</summary>
    Failure,
    
    /// <summary>Authentication required error</summary>
    Unauthorized,
    
    /// <summary>Permission denied error</summary>
    Forbidden
}

/// <summary>
/// Represents an error with code, description, type, and optional metadata.
/// </summary>
/// <remarks>
/// Errors are immutable and categorized by type for appropriate HTTP status mapping.
/// The type determines the HTTP status code when converted to API responses.
/// </remarks>
public sealed record Error
{
    /// <summary>
    /// Gets the error code for programmatic identification.
    /// </summary>
    public string Code { get; }
    
    /// <summary>
    /// Gets the human-readable error description.
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Gets the error type for categorization.
    /// </summary>
    public ErrorType Type { get; }
    
    /// <summary>
    /// Gets optional metadata associated with the error.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; }
    
    [JsonConstructor]
    private Error(string code, string description, ErrorType type, 
        Dictionary<string, object>? metadata = null)
    {
        Code = code;
        Description = description;
        Type = type;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="description">The error description</param>
    /// <param name="metadata">Optional metadata (e.g., field names)</param>
    public static Error Validation(string code, string description, 
        Dictionary<string, object>? metadata = null)
        => new(code, description, ErrorType.Validation, metadata);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="description">The error description</param>
    public static Error NotFound(string code, string description)
        => new(code, description, ErrorType.NotFound);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="description">The error description</param>
    public static Error Conflict(string code, string description)
        => new(code, description, ErrorType.Conflict);

    /// <summary>
    /// Creates a general failure error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="description">The error description</param>
    public static Error Failure(string code, string description)
        => new(code, description, ErrorType.Failure);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="description">The error description</param>
    public static Error Unauthorized(string code, string description)
        => new(code, description, ErrorType.Unauthorized);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="description">The error description</param>
    public static Error Forbidden(string code, string description)
        => new(code, description, ErrorType.Forbidden);

    /// <summary>
    /// Represents the absence of an error (used for successful results).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
}
