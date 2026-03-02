using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Extensions.Errors;
using System.Diagnostics;
using System.Text.Json;
using MinimalCleanArch.Extensions.Models;

namespace MinimalCleanArch.Extensions.Middlewares;

/// <summary>
/// Middleware that handles unhandled exceptions and returns RFC 7807 Problem Details responses.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment? _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="environment">Optional host environment for determining debug mode.</param>
    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment? environment = null)
    {
        _next = next;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment;
    }

    /// <summary>
    /// Processes the HTTP request and catches any unhandled exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

            _logger.LogError(ex,
                "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                correlationId, context.Request.Path, context.Request.Method);

            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/problem+json";

        var statusCode = ErrorResponseMapper.ResolveStatusCode(exception);
        context.Response.StatusCode = statusCode;
        var includeSensitiveDetails = _environment?.IsDevelopment() == true || Debugger.IsAttached;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ErrorResponseMapper.ResolveTitle(exception),
            Type = $"https://httpstatuses.com/{statusCode}",
            Detail = ErrorResponseMapper.ResolveDetail(exception, includeSensitiveDetails),
            Instance = context.Request.Path
        };

        foreach (var extension in ErrorResponseMapper.CreateBaseExtensions(context, exception))
        {
            problemDetails.Extensions[extension.Key] = extension.Value!;
        }

        if (!problemDetails.Extensions.ContainsKey("correlationId"))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        // Include additional debug info in development or when debugger is attached
        if (includeSensitiveDetails)
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName ?? "Unknown";
            problemDetails.Extensions["stackTrace"] = exception.StackTrace ?? string.Empty;

            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    type = exception.InnerException.GetType().FullName,
                    message = exception.InnerException.Message
                };
            }
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsJsonAsync(problemDetails, options);
    }
}
