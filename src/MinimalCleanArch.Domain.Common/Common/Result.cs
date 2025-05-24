using System.Diagnostics.CodeAnalysis;

namespace MinimalCleanArch.Domain.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error associated with the failed operation
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful</param>
    /// <param name="error">The error if the operation failed</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Successful result cannot have an error");
        
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failed result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>A successful result</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error
    /// </summary>
    /// <param name="error">The error that caused the failure</param>
    /// <returns>A failed result</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="value">The value</param>
    /// <returns>A successful result with the specified value</returns>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="error">The error that caused the failure</param>
    /// <returns>A failed result</returns>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>
    /// Implicitly converts an Error to a failed Result
    /// </summary>
    /// <param name="error">The error</param>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail
/// </summary>
/// <typeparam name="TValue">The type of the value returned on success</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>
    /// Gets the value if the operation was successful
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing value on a failed result</exception>
    public TValue Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot access value of a failed result");
            
            return _value!;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TValue}"/> class
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="isSuccess">Whether the operation was successful</param>
    /// <param name="error">The error if the operation failed</param>
    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Tries to get the value if the result is successful
    /// </summary>
    /// <param name="value">The value if successful</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        value = IsSuccess ? _value : default;
        return IsSuccess;
    }

    /// <summary>
    /// Gets the value if successful, otherwise returns the default value
    /// </summary>
    /// <param name="defaultValue">The default value to return on failure</param>
    /// <returns>The value or default value</returns>
    public TValue GetValueOrDefault(TValue defaultValue = default!) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Maps the result value to another type if successful
    /// </summary>
    /// <typeparam name="TOutput">The target type</typeparam>
    /// <param name="mapper">The mapping function</param>
    /// <returns>A result with the mapped value or the same error</returns>
    public Result<TOutput> Map<TOutput>(Func<TValue, TOutput> mapper)
    {
        if (IsFailure)
            return Result.Failure<TOutput>(Error);

        try
        {
            return Result.Success(mapper(Value));
        }
        catch (Exception ex)
        {
            return Result.Failure<TOutput>(Error.FromException(ex));
        }
    }

    /// <summary>
    /// Binds the result to another operation if successful
    /// </summary>
    /// <typeparam name="TOutput">The target type</typeparam>
    /// <param name="binder">The binding function</param>
    /// <returns>The result of the binding operation or the same error</returns>
    public Result<TOutput> Bind<TOutput>(Func<TValue, Result<TOutput>> binder)
    {
        if (IsFailure)
            return Result.Failure<TOutput>(Error);

        try
        {
            return binder(Value);
        }
        catch (Exception ex)
        {
            return Result.Failure<TOutput>(Error.FromException(ex));
        }
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The same result for chaining</returns>
    public Result<TValue> OnSuccess(Action<TValue> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Executes an action if the result is failed
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The same result for chaining</returns>
    public Result<TValue> OnFailure(Action<Error> action)
    {
        if (IsFailure)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Implicitly converts a value to a successful Result
    /// </summary>
    /// <param name="value">The value</param>
    public static implicit operator Result<TValue>(TValue value) => Result.Success(value);

    /// <summary>
    /// Implicitly converts an Error to a failed Result
    /// </summary>
    /// <param name="error">The error</param>
    public static implicit operator Result<TValue>(Error error) => Result.Failure<TValue>(error);
}

/// <summary>
/// Represents an error that can occur during an operation
/// </summary>
public class Error : IEquatable<Error>
{
    /// <summary>
    /// Represents no error
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>
    /// Gets the error code
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the error type
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Gets additional error details
    /// </summary>
    public Dictionary<string, object> Details { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <param name="type">The error type</param>
    /// <param name="details">Additional error details</param>
    public Error(string code, string message, ErrorType type = ErrorType.Failure, Dictionary<string, object>? details = null)
    {
        Code = code;
        Message = message;
        Type = type;
        Details = details ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates an error from an exception
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <returns>An error representing the exception</returns>
    public static Error FromException(Exception exception)
    {
        var details = new Dictionary<string, object>
        {
            ["ExceptionType"] = exception.GetType().Name,
            ["StackTrace"] = exception.StackTrace ?? string.Empty
        };

        var errorType = exception switch
        {
            ArgumentException => ErrorType.Validation,
            UnauthorizedAccessException => ErrorType.Unauthorized,
            KeyNotFoundException => ErrorType.NotFound,
            InvalidOperationException => ErrorType.Conflict,
            _ => ErrorType.Failure
        };

        return new Error(
            exception.GetType().Name,
            exception.Message,
            errorType,
            details);
    }

    /// <summary>
    /// Creates a validation error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <param name="field">The field that failed validation</param>
    /// <returns>A validation error</returns>
    public static Error Validation(string code, string message, string? field = null)
    {
        var details = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(field))
        {
            details["Field"] = field;
        }

        return new Error(code, message, ErrorType.Validation, details);
    }

    /// <summary>
    /// Creates a not found error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A not found error</returns>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Creates an unauthorized error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>An unauthorized error</returns>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>
    /// Creates a conflict error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A conflict error</returns>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Creates a forbidden error
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A forbidden error</returns>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public bool Equals(Error? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Code == other.Code && Type == other.Type;
    }

    public override bool Equals(object? obj) => obj is Error error && Equals(error);

    public override int GetHashCode() => HashCode.Combine(Code, Type);

    public static bool operator ==(Error? left, Error? right) => Equals(left, right);

    public static bool operator !=(Error? left, Error? right) => !Equals(left, right);

    public override string ToString() => $"[{Type}] {Code}: {Message}";
}

/// <summary>
/// Defines the types of errors that can occur
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// No error
    /// </summary>
    None = 0,

    /// <summary>
    /// General failure
    /// </summary>
    Failure = 1,

    /// <summary>
    /// Validation error
    /// </summary>
    Validation = 2,

    /// <summary>
    /// Resource not found
    /// </summary>
    NotFound = 3,

    /// <summary>
    /// Unauthorized access
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// Forbidden access
    /// </summary>
    Forbidden = 5,

    /// <summary>
    /// Conflict with current state
    /// </summary>
    Conflict = 6
}