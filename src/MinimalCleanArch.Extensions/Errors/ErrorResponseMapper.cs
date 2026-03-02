using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Domain.Exceptions;

namespace MinimalCleanArch.Extensions.Errors;

internal static class ErrorResponseMapper
{
    public static int ResolveStatusCode(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return NormalizeStatusCode(error.StatusCode);
    }

    public static int ResolveStatusCode(Exception exception)
    {
        return exception switch
        {
            DomainException domainException when domainException.Error != Error.None
                => NormalizeStatusCode(domainException.Error.StatusCode),
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            ApplicationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            OperationCanceledException => StatusCodes.Status400BadRequest,
            TimeoutException => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    public static string ResolveTitle(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return ResolveErrorTitle(error);
    }

    public static string ResolveTitle(Exception exception)
    {
        return exception switch
        {
            DomainException domainException when domainException.Error != Error.None
                => ResolveErrorTitle(domainException.Error),
            ArgumentNullException => "Invalid Request",
            ArgumentException => "Invalid Request",
            InvalidOperationException => "Invalid Operation",
            ApplicationException => "Bad Request",
            UnauthorizedAccessException => "Unauthorized",
            KeyNotFoundException => "Not Found",
            NotImplementedException => "Not Implemented",
            OperationCanceledException => "Request Cancelled",
            TimeoutException => "Gateway Timeout",
            _ => "Internal Server Error"
        };
    }

    public static string ResolveDetail(Error error, bool includeSensitiveDetails)
    {
        ArgumentNullException.ThrowIfNull(error);
        var statusCode = ResolveStatusCode(error);
        if (!includeSensitiveDetails && statusCode == StatusCodes.Status500InternalServerError)
        {
            return "An unexpected error occurred. Please try again later.";
        }

        return error.Message;
    }

    public static string ResolveDetail(Exception exception, bool includeSensitiveDetails)
    {
        var statusCode = ResolveStatusCode(exception);

        if (exception is DomainException domainException && domainException.Error != Error.None)
        {
            return ResolveDetail(domainException.Error, includeSensitiveDetails);
        }

        if (!includeSensitiveDetails && statusCode == StatusCodes.Status500InternalServerError)
        {
            return "An unexpected error occurred. Please try again later.";
        }

        return exception.Message;
    }

    public static Dictionary<string, object?> CreateBaseExtensions(HttpContext context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (exception is DomainException domainException && domainException.Error != Error.None)
        {
            return CreateBaseExtensions(context, domainException.Error);
        }

        var extensions = new Dictionary<string, object?>
        {
            ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
        };

        if (context.Items.TryGetValue("CorrelationId", out var correlationId) && correlationId is not null)
        {
            extensions["correlationId"] = correlationId.ToString();
        }

        return extensions;
    }

    public static Dictionary<string, object?> CreateBaseExtensions(HttpContext context, Error error)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(error);

        var extensions = new Dictionary<string, object?>
        {
            ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
        };

        if (context.Items.TryGetValue("CorrelationId", out var correlationId) && correlationId is not null)
        {
            extensions["correlationId"] = correlationId.ToString();
        }

        extensions["code"] = error.Code;
        extensions["errorType"] = error.Type.ToString();
        if (error.Metadata.Count > 0)
        {
            extensions["metadata"] = error.Metadata;
        }

        return extensions;
    }

    private static int NormalizeStatusCode(int statusCode) =>
        statusCode is >= 100 and <= 599
            ? statusCode
            : StatusCodes.Status500InternalServerError;

    private static string ResolveErrorTitle(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => "Validation Error",
            ErrorType.NotFound => "Not Found",
            ErrorType.Unauthorized or ErrorType.Authentication => "Unauthorized",
            ErrorType.Forbidden or ErrorType.Authorization => "Forbidden",
            ErrorType.Conflict => "Conflict",
            ErrorType.BusinessRule => "Business Rule Violation",
            ErrorType.External => "External Service Error",
            ErrorType.RateLimit => "Rate Limit Exceeded",
            _ => "Internal Server Error"
        };
    }
}
