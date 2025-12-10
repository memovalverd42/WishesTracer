namespace WishesTracer.Shared.Exceptions;

public abstract class WishesTracerException : Exception
{
    protected WishesTracerException(string message) : base(message)
    {
    }

    protected WishesTracerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
