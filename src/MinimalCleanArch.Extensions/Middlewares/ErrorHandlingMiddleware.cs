using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
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

        var statusCode = GetStatusCode(exception);
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = GetTitle(exception),
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Detail = GetDetail(exception, statusCode),
            Instance = context.Request.Path
        };

        // Always include correlation ID and trace ID
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        // Include additional debug info in development or when debugger is attached
        var isDevelopment = _environment?.IsDevelopment() == true || Debugger.IsAttached;
        if (isDevelopment)
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

    private static HttpStatusCode GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            ApplicationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            NotImplementedException => HttpStatusCode.NotImplemented,
            OperationCanceledException => HttpStatusCode.BadRequest,
            TimeoutException => HttpStatusCode.GatewayTimeout,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
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

    private string GetDetail(Exception exception, HttpStatusCode statusCode)
    {
        // Don't expose internal error details in production
        if (statusCode == HttpStatusCode.InternalServerError &&
            _environment?.IsDevelopment() != true &&
            !Debugger.IsAttached)
        {
            return "An unexpected error occurred. Please try again later.";
        }

        return exception.Message;
    }
}
