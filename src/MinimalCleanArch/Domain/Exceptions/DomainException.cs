using MinimalCleanArch.Domain.Common;

namespace MinimalCleanArch.Domain.Exceptions;

/// <summary>
/// Exception for domain validation and business-rule errors.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Gets the structured error associated with this exception.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    public DomainException()
        : this(Error.Failure("DOMAIN_ERROR", "A domain error occurred."))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message.
    /// </summary>
    public DomainException(string message)
        : this(Error.Validation("DOMAIN_ERROR", message))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message and inner exception.
    /// </summary>
    public DomainException(string message, Exception innerException)
        : this(Error.Failure("DOMAIN_ERROR", message), innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a structured error.
    /// </summary>
    public DomainException(Error error)
        : base((error ?? throw new ArgumentNullException(nameof(error))).Message)
    {
        Error = error;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a structured error and inner exception.
    /// </summary>
    public DomainException(Error error, Exception innerException)
        : base((error ?? throw new ArgumentNullException(nameof(error))).Message, innerException)
    {
        Error = error;
    }
}
