namespace MinimalCleanArch.Domain.Exceptions;

/// <summary>
/// Exception for domain validation errors
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class
    /// </summary>
    public DomainException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with the specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public DomainException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with the specified error message
    /// and a reference to the inner exception that is the cause of this exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}
