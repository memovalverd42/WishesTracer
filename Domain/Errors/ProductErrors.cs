// ProductErrors.cs - Domain error definitions for product-related operations
using WishesTracer.Shared.Results;

namespace WishesTracer.Domain.Errors;

/// <summary>
/// Contains domain-specific error definitions for product operations.
/// </summary>
/// <remarks>
/// Provides strongly-typed errors following the Result pattern for product validation
/// and business rule violations. Errors are categorized by type (NotFound, Validation, 
/// Conflict) for appropriate HTTP status code mapping.
/// </remarks>
public static class ProductErrors
{
    /// <summary>
    /// Creates a NotFound error when a product with the specified ID doesn't exist.
    /// </summary>
    /// <param name="id">The product ID that was not found</param>
    /// <returns>A NotFound error with product details</returns>
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "Product.NotFound",
            $"Product with ID '{id}' was not found");

    /// <summary>
    /// Gets a validation error for invalid product prices (zero or negative).
    /// </summary>
    public static Error InvalidPrice =>
        Error.Validation(
            "Product.InvalidPrice",
            "Price must be greater than zero");

    /// <summary>
    /// Creates a conflict error when attempting to create a product with a duplicate URL.
    /// </summary>
    /// <param name="url">The URL that already exists in the system</param>
    /// <returns>A Conflict error indicating duplicate URL</returns>
    public static Error DuplicateUrl(string url) =>
        Error.Conflict(
            "Product.DuplicateUrl",
            $"A product with URL '{url}' already exists");

    /// <summary>
    /// Gets a validation error for malformed or invalid URLs.
    /// </summary>
    public static Error InvalidUrl =>
        Error.Validation(
            "Product.InvalidUrl",
            "The provided URL is not valid",
            new Dictionary<string, object> { ["Field"] = "Url" });
}
