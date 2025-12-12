// WishesTracerException.cs - Base exception class for domain exceptions
namespace WishesTracer.Shared.Exceptions;

/// <summary>
/// Base exception class for all WishesTracer-specific exceptions.
/// </summary>
/// <remarks>
/// Provides a common base for domain-specific exceptions, enabling centralized
/// exception handling and filtering. Inherit from this class to create custom
/// exceptions that should be handled specially by the application.
/// </remarks>
public abstract class WishesTracerException : Exception
{
    /// <summary>
    /// Initializes a new instance of the WishesTracerException class.
    /// </summary>
    /// <param name="message">The exception message</param>
    protected WishesTracerException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the WishesTracerException class with inner exception.
    /// </summary>
    /// <param name="message">The exception message</param>
    /// <param name="innerException">The inner exception that caused this exception</param>
    protected WishesTracerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
