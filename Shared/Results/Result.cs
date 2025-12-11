// Result.cs - Result pattern implementation for error handling
using System.Text.Json.Serialization;

namespace WishesTracer.Shared.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
/// <remarks>
/// This implements the Result pattern (Railway-Oriented Programming) to handle
/// errors functionally without exceptions. Enforces that success results have no error
/// and failure results must have an error.
/// </remarks>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the Result class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded</param>
    /// <param name="error">The error if the operation failed</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if success result has error or failure result has no error
    /// </exception>
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

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonIgnore] 
    public bool IsFailure => !IsSuccess;
    
    /// <summary>
    /// Gets the error details if the operation failed.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result without a value.
    /// </summary>
    public static Result Success() => new(true, Error.None);
    
    /// <summary>
    /// Creates a failed result with an error.
    /// </summary>
    /// <param name="error">The error that caused the failure</param>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="value">The success value</param>
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    
    /// <summary>
    /// Creates a failed result with an error.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="error">The error that caused the failure</param>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    /// <summary>
    /// Implicitly converts an Error to a failed Result.
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Initializes a new instance of the Result{T} class.
    /// </summary>
    [JsonConstructor]
    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the result value. Returns default if the result failed.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : default!;

    /// <summary>
    /// Gets the result value or throws an exception if the result failed.
    /// </summary>
    /// <returns>The success value</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to get value from a failed result
    /// </exception>
    public T GetValueOrThrow() 
    {
        if (IsFailure) 
            throw new InvalidOperationException("Cannot access value of a failed result");
        return _value!;
    }

    /// <summary>
    /// Implicitly converts a value to a successful Result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
    
    /// <summary>
    /// Implicitly converts an Error to a failed Result.
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
