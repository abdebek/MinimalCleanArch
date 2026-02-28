using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace MinimalCleanArch.Domain.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error associated with the failed operation.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful.</param>
    /// <param name="error">The error if the operation failed.</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Successful result cannot have an error");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failed result must have an error");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a failed result from code/message/type values.
    /// </summary>
    public static Result Failure(
        string code,
        string message,
        ErrorType type = ErrorType.Failure,
        int? statusCode = null,
        Dictionary<string, object>? details = null) =>
        new(false, new Error(code, message, type, details, statusCode));

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>
    /// Creates a failed result with the specified error values.
    /// </summary>
    public static Result<TValue> Failure<TValue>(
        string code,
        string message,
        ErrorType type = ErrorType.Failure,
        int? statusCode = null,
        Dictionary<string, object>? details = null) =>
        new(default, false, new Error(code, message, type, details, statusCode));

    /// <summary>
    /// Matches the result and returns a value.
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error);

    /// <summary>
    /// Chains another result operation when current result is successful.
    /// </summary>
    public Result Bind(Func<Result> next) => IsSuccess ? next() : this;

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> to a failed result.
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail.
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot access value of a failed result");
            }

            return _value!;
        }
    }

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Tries to get the value if the result is successful.
    /// </summary>
    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        value = IsSuccess ? _value : default;
        return IsSuccess;
    }

    /// <summary>
    /// Gets the value if successful, otherwise returns the default value.
    /// </summary>
    public TValue GetValueOrDefault(TValue defaultValue = default!) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Matches the result and returns a value.
    /// </summary>
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    /// <summary>
    /// Maps the result value to another type if successful.
    /// </summary>
    public Result<TOutput> Map<TOutput>(Func<TValue, TOutput> mapper)
    {
        if (IsFailure)
        {
            return Result.Failure<TOutput>(Error);
        }

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
    /// Binds the result to another operation if successful.
    /// </summary>
    public Result<TOutput> Bind<TOutput>(Func<TValue, Result<TOutput>> binder)
    {
        if (IsFailure)
        {
            return Result.Failure<TOutput>(Error);
        }

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
    /// Converts this generic result to a non-generic result.
    /// </summary>
    public Result ToResult() => IsSuccess ? Result.Success() : Result.Failure(Error);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result<TValue> OnSuccess(Action<TValue> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is failed.
    /// </summary>
    public Result<TValue> OnFailure(Action<Error> action)
    {
        if (IsFailure)
        {
            action(Error);
        }

        return this;
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<TValue>(TValue value) => Result.Success(value);

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> to a failed result.
    /// </summary>
    public static implicit operator Result<TValue>(Error error) => Result.Failure<TValue>(error);

}

/// <summary>
/// Represents an error that can occur during an operation.
/// </summary>
public class Error : IEquatable<Error>
{
    /// <summary>
    /// Represents no error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None, statusCode: (int)HttpStatusCode.OK);

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the error type.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Gets the HTTP status code for this error.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets additional error details.
    /// </summary>
    public Dictionary<string, object> Details { get; }

    /// <summary>
    /// Gets additional error metadata as a read-only view.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata => Details;

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    public Error(
        string code,
        string message,
        ErrorType type = ErrorType.Failure,
        Dictionary<string, object>? details = null)
        : this(code, message, type, details, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    public Error(
        string code,
        string message,
        ErrorType type,
        Dictionary<string, object>? details = null,
        int? statusCode = null)
    {
        Code = code;
        Message = message;
        Type = type;
        StatusCode = statusCode ?? GetDefaultStatusCode(type);
        Details = details is null ? new Dictionary<string, object>() : new Dictionary<string, object>(details);
    }

    /// <summary>
    /// Returns a copy of this error with the provided metadata key/value.
    /// </summary>
    public Error WithMetadata(string key, object value)
    {
        var details = new Dictionary<string, object>(Details)
        {
            [key] = value
        };

        return new Error(Code, Message, Type, details, StatusCode);
    }

    /// <summary>
    /// Creates an error from an exception.
    /// </summary>
    public static Error FromException(Exception exception)
    {
        var details = new Dictionary<string, object>
        {
            ["ExceptionType"] = exception.GetType().Name,
            ["StackTrace"] = exception.StackTrace ?? string.Empty
        };

        var (errorType, statusCode) = exception switch
        {
            ArgumentException => (ErrorType.Validation, (int)HttpStatusCode.BadRequest),
            UnauthorizedAccessException => (ErrorType.Unauthorized, (int)HttpStatusCode.Unauthorized),
            KeyNotFoundException => (ErrorType.NotFound, (int)HttpStatusCode.NotFound),
            InvalidOperationException => (ErrorType.Conflict, (int)HttpStatusCode.Conflict),
            TimeoutException => (ErrorType.External, (int)HttpStatusCode.GatewayTimeout),
            _ => (ErrorType.SystemError, (int)HttpStatusCode.InternalServerError)
        };

        return new Error(
            exception.GetType().Name,
            exception.Message,
            errorType,
            details,
            statusCode);
    }

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string code, string message, string? field = null)
    {
        var details = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(field))
        {
            details["Field"] = field;
        }

        return new Error(code, message, ErrorType.Validation, details, (int)HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound, statusCode: (int)HttpStatusCode.NotFound);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized, statusCode: (int)HttpStatusCode.Unauthorized);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden, statusCode: (int)HttpStatusCode.Forbidden);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict, statusCode: (int)HttpStatusCode.Conflict);

    /// <summary>
    /// Creates an authentication-required error.
    /// </summary>
    public static Error Authentication(string code, string message) =>
        new(code, message, ErrorType.Authentication, statusCode: (int)HttpStatusCode.Unauthorized);

    /// <summary>
    /// Creates an authorization error.
    /// </summary>
    public static Error Authorization(string code, string message) =>
        new(code, message, ErrorType.Authorization, statusCode: (int)HttpStatusCode.Forbidden);

    /// <summary>
    /// Creates a business-rule error.
    /// </summary>
    public static Error BusinessRule(string code, string message) =>
        new(code, message, ErrorType.BusinessRule, statusCode: 422);

    /// <summary>
    /// Creates a system/internal error.
    /// </summary>
    public static Error SystemError(string code, string message) =>
        new(code, message, ErrorType.SystemError, statusCode: (int)HttpStatusCode.InternalServerError);

    /// <summary>
    /// Creates an external dependency error.
    /// </summary>
    public static Error External(string code, string message) =>
        new(code, message, ErrorType.External, statusCode: (int)HttpStatusCode.BadGateway);

    /// <summary>
    /// Creates a rate-limit error.
    /// </summary>
    public static Error RateLimit(string code, string message) =>
        new(code, message, ErrorType.RateLimit, statusCode: 429);

    /// <summary>
    /// Creates a general failure error.
    /// </summary>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure, statusCode: (int)HttpStatusCode.InternalServerError);

    /// <summary>
    /// Creates a custom error.
    /// </summary>
    public static Error Custom(
        string code,
        string message,
        ErrorType type,
        int statusCode,
        Dictionary<string, object>? details = null) =>
        new(code, message, type, details, statusCode);

    public bool Equals(Error? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Code == other.Code && Type == other.Type;
    }

    public override bool Equals(object? obj) => obj is Error error && Equals(error);

    public override int GetHashCode() => HashCode.Combine(Code, Type);

    public static bool operator ==(Error? left, Error? right) => Equals(left, right);

    public static bool operator !=(Error? left, Error? right) => !Equals(left, right);

    public override string ToString() => $"[{Type}] {Code}: {Message}";

    private static int GetDefaultStatusCode(ErrorType type)
    {
        return type switch
        {
            ErrorType.None => (int)HttpStatusCode.OK,
            ErrorType.Validation => (int)HttpStatusCode.BadRequest,
            ErrorType.NotFound => (int)HttpStatusCode.NotFound,
            ErrorType.Unauthorized => (int)HttpStatusCode.Unauthorized,
            ErrorType.Forbidden => (int)HttpStatusCode.Forbidden,
            ErrorType.Conflict => (int)HttpStatusCode.Conflict,
            ErrorType.Authentication => (int)HttpStatusCode.Unauthorized,
            ErrorType.Authorization => (int)HttpStatusCode.Forbidden,
            ErrorType.BusinessRule => 422,
            ErrorType.External => (int)HttpStatusCode.BadGateway,
            ErrorType.RateLimit => 429,
            ErrorType.SystemError => (int)HttpStatusCode.InternalServerError,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }
}

/// <summary>
/// Defines the types of errors that can occur.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// No error.
    /// </summary>
    None = 0,

    /// <summary>
    /// General failure.
    /// </summary>
    Failure = 1,

    /// <summary>
    /// Validation error.
    /// </summary>
    Validation = 2,

    /// <summary>
    /// Resource not found.
    /// </summary>
    NotFound = 3,

    /// <summary>
    /// Unauthorized access.
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// Forbidden access.
    /// </summary>
    Forbidden = 5,

    /// <summary>
    /// Conflict with current state.
    /// </summary>
    Conflict = 6,

    /// <summary>
    /// Authentication required or failed.
    /// </summary>
    Authentication = 7,

    /// <summary>
    /// Authorization failed.
    /// </summary>
    Authorization = 8,

    /// <summary>
    /// Business rule violation.
    /// </summary>
    BusinessRule = 9,

    /// <summary>
    /// System/internal error.
    /// </summary>
    SystemError = 10,

    /// <summary>
    /// External dependency failure.
    /// </summary>
    External = 11,

    /// <summary>
    /// Rate limit exceeded.
    /// </summary>
    RateLimit = 12
}
