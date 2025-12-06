using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MinimalCleanArch.Audit.Services;

/// <summary>
/// Default implementation of <see cref="IAuditContextProvider"/> using HttpContext.
/// </summary>
public class HttpContextAuditContextProvider : IAuditContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextAuditContextProvider"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public HttpContextAuditContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
    }

    /// <inheritdoc />
    public string? GetUserName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.Identity?.Name;
    }

    /// <inheritdoc />
    public string? GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return null;

        // Try to get from Items (set by CorrelationIdMiddleware)
        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
            return correlationId?.ToString();

        // Fall back to TraceIdentifier
        return context.TraceIdentifier;
    }

    /// <inheritdoc />
    public string? GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return null;

        // Check for forwarded header first
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <inheritdoc />
    public string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
    }

    /// <inheritdoc />
    public IDictionary<string, object>? GetMetadata()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return null;

        return new Dictionary<string, object>
        {
            ["RequestPath"] = context.Request.Path.ToString(),
            ["RequestMethod"] = context.Request.Method
        };
    }
}
