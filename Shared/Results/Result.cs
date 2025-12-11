using System.Text.Json.Serialization;

namespace WishesTracer.Shared.Results;

public class Result
{
    [JsonConstructor]
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    
    [JsonIgnore] 
    public bool IsFailure => !IsSuccess;
    
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    // Esto le dice a System.Text.Json: "Usa este constructor aunque sea protected/internal"
    [JsonConstructor]
    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : default!;

    public T GetValueOrThrow() 
    {
        if (IsFailure) throw new InvalidOperationException("Cannot access value of a failed result");
        return _value!;
    }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
