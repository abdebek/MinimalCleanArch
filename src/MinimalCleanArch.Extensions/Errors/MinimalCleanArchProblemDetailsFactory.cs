using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalCleanArch.Domain.Common;

namespace MinimalCleanArch.Extensions.Errors;

internal static class MinimalCleanArchProblemDetailsFactory
{
    public static ProblemDetails CreateForError(
        HttpContext context,
        Error error,
        bool includeSensitiveDetails,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = ErrorResponseMapper.ResolveStatusCode(error);
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ErrorResponseMapper.ResolveTitle(error),
            Type = $"https://httpstatuses.com/{statusCode}",
            Detail = ErrorResponseMapper.ResolveDetail(error, includeSensitiveDetails),
            Instance = instance ?? context.Request.Path
        };

        Enrich(problemDetails, ErrorResponseMapper.CreateBaseExtensions(context, error));
        return problemDetails;
    }

    public static ProblemDetails CreateForException(
        HttpContext context,
        Exception exception,
        bool includeSensitiveDetails,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);

        var statusCode = ErrorResponseMapper.ResolveStatusCode(exception);
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ErrorResponseMapper.ResolveTitle(exception),
            Type = $"https://httpstatuses.com/{statusCode}",
            Detail = ErrorResponseMapper.ResolveDetail(exception, includeSensitiveDetails),
            Instance = instance ?? context.Request.Path
        };

        Enrich(problemDetails, ErrorResponseMapper.CreateBaseExtensions(context, exception));

        if (!problemDetails.Extensions.ContainsKey("correlationId"))
        {
            problemDetails.Extensions["correlationId"] =
                context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        }

        if (includeSensitiveDetails)
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName ?? "Unknown";
            problemDetails.Extensions["stackTrace"] = exception.StackTrace ?? string.Empty;

            if (exception.InnerException is not null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    type = exception.InnerException.GetType().FullName,
                    message = exception.InnerException.Message
                };
            }
        }

        return problemDetails;
    }

    public static ValidationProblemDetails CreateValidation(
        HttpContext context,
        IDictionary<string, string[]> errors,
        string? detail = null,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(errors);

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Type = "https://httpstatuses.com/400",
            Detail = detail ?? "One or more validation errors occurred",
            Instance = instance ?? context.Request.Path
        };

        EnrichCommon(context, problemDetails);
        return problemDetails;
    }

    public static ProblemDetails CreateRateLimit(
        HttpContext context,
        int? retryAfterSeconds,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var error = Error.RateLimit("RATE_LIMIT_EXCEEDED", "Rate limit exceeded. Please try again later.");
        if (retryAfterSeconds.HasValue)
        {
            error = error.WithMetadata("RetryAfterSeconds", retryAfterSeconds.Value);
        }

        var problemDetails = CreateForError(context, error, includeSensitiveDetails: false, instance);
        problemDetails.Extensions["retryAfter"] = retryAfterSeconds;
        return problemDetails;
    }

    public static void EnrichCommon(HttpContext context, ProblemDetails problemDetails)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(problemDetails);

        problemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.TraceIdentifier);

        if (context.Items.TryGetValue("CorrelationId", out var correlationId) && correlationId is not null)
        {
            problemDetails.Extensions.TryAdd("correlationId", correlationId.ToString());
        }
    }

    private static void Enrich(ProblemDetails problemDetails, IReadOnlyDictionary<string, object?> extensions)
    {
        foreach (var extension in extensions)
        {
            problemDetails.Extensions[extension.Key] = extension.Value;
        }
    }
}
