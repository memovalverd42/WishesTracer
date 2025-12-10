namespace WishesTracer.Shared.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Failure,
    Unauthorized,
    Forbidden
}

public sealed record Error
{
    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }
    public Dictionary<string, object>? Metadata { get; }

    private Error(string code, string description, ErrorType type, Dictionary<string, object>? metadata = null)
    {
        Code = code;
        Description = description;
        Type = type;
        Metadata = metadata;
    }

    public static Error Validation(string code, string description, Dictionary<string, object>? metadata = null)
        => new(code, description, ErrorType.Validation, metadata);

    public static Error NotFound(string code, string description)
        => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description)
        => new(code, description, ErrorType.Conflict);

    public static Error Failure(string code, string description)
        => new(code, description, ErrorType.Failure);

    public static Error Unauthorized(string code, string description)
        => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description)
        => new(code, description, ErrorType.Forbidden);

    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
}
