using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace MinimalCleanArch.Extensions.Middlewares;

/// <summary>
/// Middleware that adds correlation ID support for distributed tracing.
/// Reads correlation ID from incoming request headers or generates a new one,
/// and includes it in the response headers and log context.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>
    /// Default header name for the correlation ID.
    /// </summary>
    public const string DefaultHeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private readonly string _headerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="headerName">Optional custom header name for the correlation ID.</param>
    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger,
        string? headerName = null)
    {
        _next = next;
        _logger = logger;
        _headerName = headerName ?? DefaultHeaderName;
    }

    /// <summary>
    /// Processes the HTTP request, ensuring a correlation ID is present.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(_headerName))
            {
                context.Response.Headers[_headerName] = correlationId;
            }
            return Task.CompletedTask;
        });

        // Store in HttpContext.Items for access elsewhere
        context.Items["CorrelationId"] = correlationId;

        // Push to Serilog LogContext so all logs within this request include it
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogDebug("Processing request with correlation ID: {CorrelationId}", correlationId);
            await _next(context);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get from request header
        if (context.Request.Headers.TryGetValue(_headerName, out var existingId) &&
            !string.IsNullOrWhiteSpace(existingId))
        {
            return existingId.ToString();
        }

        // Fall back to trace identifier if available
        if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
        {
            return context.TraceIdentifier;
        }

        // Generate new GUID
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Service for accessing the current correlation ID.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the current correlation ID, or null if not available.
    /// </summary>
    string? CorrelationId { get; }
}

/// <summary>
/// Default implementation of <see cref="ICorrelationIdAccessor"/> using HttpContext.
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdAccessor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
}
