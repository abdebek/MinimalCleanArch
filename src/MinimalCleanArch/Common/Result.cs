using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;

namespace MinimalCleanArch.Domain.Common;

/// <summary>
/// Marker case type representing a successful operation with no payload value.
/// </summary>
public readonly record struct Success;

/// <summary>
/// Non-boxing C# 15 union representing an operation that either succeeds (<see cref="Success"/>)
/// or fails with an <see cref="Error"/>.
/// </summary>
[Union]
public readonly struct Result
{
    private static readonly Error UninitializedError = Error.Failure(
        "RESULT_UNINITIALIZED",
        "Result was not initialized.");

    private readonly Error? _error;
    private readonly bool _isSuccess;

    /// <summary>
    /// Initializes a successful <see cref="Result"/>.
    /// </summary>
    public Result(Success _)
    {
        _isSuccess = true;
        _error = null;
    }

    /// <summary>
    /// Initializes a failed <see cref="Result"/> with the specified error.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    public Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error == Error.None)
        {
            throw new InvalidOperationException("Failed result must have an error");
        }

        _isSuccess = false;
        _error = error;
    }

    /// <summary>
    /// Gets the active union case value (<see cref="Success"/> or <see cref="Error"/>) for C# 15 union pattern matching.
    /// </summary>
    public object? Value => _isSuccess ? default(Success) : _error;

    /// <summary>
    /// Gets a value indicating whether this union holds an initialized case value.
    /// </summary>
    public bool HasValue => _isSuccess || _error is not null;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// Gets the error associated with a failed operation, or <see cref="Error.None"/> if successful.
    /// </summary>
    public Error Error => _isSuccess ? Error.None : (_error ?? UninitializedError);

    /// <summary>
    /// Non-boxing union accessor for the <see cref="Success"/> case.
    /// </summary>
    public bool TryGetValue(out Success value)
    {
        value = default;
        return _isSuccess;
    }

    /// <summary>
    /// Non-boxing union accessor for the <see cref="Error"/> case.
    /// </summary>
    public bool TryGetValue([NotNullWhen(true)] out Error? error)
    {
        if (!_isSuccess)
        {
            error = _error ?? UninitializedError;
            return true;
        }

        error = null;
        return false;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(default(Success));

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result from code/message/type values.
    /// </summary>
    public static Result Failure(
        string code,
        string message,
        ErrorType type = ErrorType.Failure,
        int? statusCode = null,
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(new Error(code, message, type, metadata, statusCode));

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<TValue> Success<TValue>(TValue value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<TValue> Failure<TValue>(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error values.
    /// </summary>
    public static Result<TValue> Failure<TValue>(
        string code,
        string message,
        ErrorType type = ErrorType.Failure,
        int? statusCode = null,
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(new Error(code, message, type, metadata, statusCode));

    /// <summary>
    /// Matches the result and returns a value.
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return _isSuccess ? onSuccess() : onFailure(Error);
    }

    /// <summary>
    /// Chains another result operation when the current result is successful.
    /// </summary>
    public Result Bind(Func<Result> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return _isSuccess ? next() : this;
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result OnSuccess(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_isSuccess)
        {
            action();
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is failed.
    /// </summary>
    public Result OnFailure(Action<Error> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!_isSuccess)
        {
            action(Error);
        }

        return this;
    }
}

/// <summary>
/// Non-boxing C# 15 union representing an operation that either succeeds with a <typeparamref name="TValue"/>
/// or fails with an <see cref="Error"/>.
/// </summary>
/// <typeparam name="TValue">The success payload type.</typeparam>
[Union]
public readonly struct Result<TValue>
{
    private static readonly Error UninitializedError = Error.Failure(
        "RESULT_UNINITIALIZED",
        "Result was not initialized.");

    private readonly TValue? _value;
    private readonly Error? _error;
    private readonly bool _isSuccess;

    /// <summary>
    /// Initializes a successful <see cref="Result{TValue}"/> with the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    public Result(TValue value)
    {
        _value = value;
        _error = null;
        _isSuccess = true;
    }

    /// <summary>
    /// Initializes a failed <see cref="Result{TValue}"/> with the specified error.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    public Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error == Error.None)
        {
            throw new InvalidOperationException("Failed result must have an error");
        }

        _value = default;
        _error = error;
        _isSuccess = false;
    }

    /// <summary>
    /// Gets the active union case value (<typeparamref name="TValue"/> or <see cref="Error"/>) for C# 15 union pattern matching.
    /// Prefer C# 15 pattern matching, <see cref="TryGetValue(out TValue)"/>, or <see cref="GetValueOrThrow"/> for typed access.
    /// </summary>
    public object? Value => _isSuccess ? _value : _error;

    /// <summary>
    /// Gets a value indicating whether this union holds an initialized case value.
    /// </summary>
    public bool HasValue => _isSuccess || _error is not null;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// Gets the error associated with a failed operation, or <see cref="Error.None"/> if successful.
    /// </summary>
    public Error Error => _isSuccess ? Error.None : (_error ?? UninitializedError);

    /// <summary>
    /// Non-boxing union accessor for the <typeparamref name="TValue"/> success case.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        if (_isSuccess)
        {
            value = _value!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Non-boxing union accessor for the <see cref="Error"/> failure case.
    /// </summary>
    public bool TryGetValue([NotNullWhen(true)] out Error? error)
    {
        if (!_isSuccess)
        {
            error = _error ?? UninitializedError;
            return true;
        }

        error = null;
        return false;
    }

    /// <summary>
    /// Returns the success value, or throws <see cref="InvalidOperationException"/> if the result failed.
    /// </summary>
    public TValue GetValueOrThrow()
    {
        if (!_isSuccess)
        {
            throw new InvalidOperationException($"Cannot access value of a failed result: {Error}");
        }

        return _value!;
    }

    /// <summary>
    /// Gets the value if successful, otherwise returns the provided default value.
    /// </summary>
    public TValue GetValueOrDefault(TValue defaultValue = default!) => _isSuccess ? _value! : defaultValue;

    /// <summary>
    /// Matches the result and returns a value.
    /// </summary>
    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return _isSuccess ? onSuccess(_value!) : onFailure(Error);
    }

    /// <summary>
    /// Maps the result value to another type if successful.
    /// Unexpected exceptions thrown by <paramref name="mapper"/> propagate rather than being swallowed.
    /// </summary>
    public Result<TOutput> Map<TOutput>(Func<TValue, TOutput> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return _isSuccess
            ? new Result<TOutput>(mapper(_value!))
            : new Result<TOutput>(Error);
    }

    /// <summary>
    /// Binds the result to another operation if successful.
    /// Unexpected exceptions thrown by <paramref name="binder"/> propagate rather than being swallowed.
    /// </summary>
    public Result<TOutput> Bind<TOutput>(Func<TValue, Result<TOutput>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return _isSuccess
            ? binder(_value!)
            : new Result<TOutput>(Error);
    }

    /// <summary>
    /// Converts this generic result to a non-generic <see cref="Result"/>.
    /// </summary>
    public Result ToResult() => _isSuccess ? Result.Success() : Result.Failure(Error);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result<TValue> OnSuccess(Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_isSuccess)
        {
            action(_value!);
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is failed.
    /// </summary>
    public Result<TValue> OnFailure(Action<Error> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!_isSuccess)
        {
            action(Error);
        }

        return this;
    }
}

/// <summary>
/// Represents an immutable structured error that can occur during an operation.
/// </summary>
public sealed class Error : IEquatable<Error>
{
    private static readonly IReadOnlyDictionary<string, object> EmptyMetadata =
        new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

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
    /// Gets additional immutable error metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    public Error(
        string code,
        string message,
        ErrorType type = ErrorType.Failure,
        IReadOnlyDictionary<string, object>? metadata = null)
        : this(code, message, type, metadata, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    public Error(
        string code,
        string message,
        ErrorType type,
        IReadOnlyDictionary<string, object>? metadata = null,
        int? statusCode = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Type = type;
        StatusCode = statusCode ?? GetDefaultStatusCode(type);
        Metadata = metadata is null || metadata.Count == 0
            ? EmptyMetadata
            : new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(metadata));
    }

    /// <summary>
    /// Returns a copy of this error with the provided metadata key/value.
    /// </summary>
    public Error WithMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var copy = new Dictionary<string, object>(Metadata)
        {
            [key] = value
        };

        return new Error(Code, Message, Type, copy, StatusCode);
    }

    /// <summary>
    /// Creates an error from an exception.
    /// Stack traces are omitted by default so internal details are not accidentally exposed to callers.
    /// </summary>
    public static Error FromException(Exception exception, bool includeStackTrace = false)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Dictionary<string, object>? metadata = null;
        if (includeStackTrace)
        {
            metadata = new Dictionary<string, object>
            {
                ["ExceptionType"] = exception.GetType().Name,
                ["StackTrace"] = exception.StackTrace ?? string.Empty
            };
        }

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
            metadata,
            statusCode);
    }

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string code, string message, string? field = null)
    {
        Dictionary<string, object>? metadata = null;
        if (!string.IsNullOrWhiteSpace(field))
        {
            metadata = new Dictionary<string, object>
            {
                ["Field"] = field
            };
        }

        return new Error(code, message, ErrorType.Validation, metadata, (int)HttpStatusCode.BadRequest);
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
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new(code, message, type, metadata, statusCode);

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
