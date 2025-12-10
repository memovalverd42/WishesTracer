using WishesTracer.Shared.Results;

namespace WishesTracer.Domain.Errors;

public static class ProductErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "Product.NotFound",
            $"Product with ID '{id}' was not found");

    public static Error InvalidPrice =>
        Error.Validation(
            "Product.InvalidPrice",
            "Price must be greater than zero");

    public static Error DuplicateUrl(string url) =>
        Error.Conflict(
            "Product.DuplicateUrl",
            $"A product with URL '{url}' already exists");

    public static Error InvalidUrl =>
        Error.Validation(
            "Product.InvalidUrl",
            "The provided URL is not valid",
            new Dictionary<string, object> { ["Field"] = "Url" });
}
