using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MinimalCleanArch.Execution;
using Wolverine;

namespace MinimalCleanArch.Messaging.Execution;

internal sealed class MessagingExecutionContext : IExecutionContext
{
    private const string UserIdHeader = "mca-user-id";
    private const string UserNameHeader = "mca-user-name";
    private const string TenantIdHeader = "mca-tenant-id";

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>();

    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IMessageContext? _messageContext;
    private readonly ExecutionContextOptions _options;
    private IReadOnlyDictionary<string, string>? _metadata;

    public MessagingExecutionContext(
        IHttpContextAccessor? httpContextAccessor = null,
        IMessageContext? messageContext = null,
        IOptions<ExecutionContextOptions>? options = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _messageContext = messageContext;
        _options = options?.Value ?? new ExecutionContextOptions();
    }

    public string? UserId => GetHeader(UserIdHeader) ?? GetHttpUserId();

    public string? UserName => GetHeader(UserNameHeader) ?? GetHttpUserName();

    public string? TenantId => GetMessageEnvelope()?.TenantId ?? GetHeader(TenantIdHeader) ?? GetHttpTenantId();

    public string? CorrelationId => GetMessageEnvelope()?.CorrelationId ?? GetHttpCorrelationId();

    public string? ClientIpAddress => GetHttpClientIpAddress();

    public string? UserAgent => _httpContextAccessor?.HttpContext?.Request.Headers.UserAgent.ToString();

    public IReadOnlyDictionary<string, string> Metadata
    {
        get => _metadata ??= BuildMetadata();
    }

    private string? GetHeader(string key)
    {
        var envelope = GetMessageEnvelope();
        if (envelope?.Headers is null)
        {
            return null;
        }

        return envelope.Headers.TryGetValue(key, out var value) ? value : null;
    }

    private Envelope? GetMessageEnvelope() => _messageContext?.Envelope;

    private string? GetHttpUserId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return FindClaimValue(user, _options.UserIdClaimTypes);
    }

    private string? GetHttpUserName()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return FindClaimValue(user, _options.UserNameClaimTypes)
            ?? user.Identity?.Name
            ?? FindClaimValue(user, _options.UserNameFallbackClaimTypes);
    }

    private string? GetHttpTenantId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return FindClaimValue(user, _options.TenantIdClaimTypes);
    }

    private string? GetHttpCorrelationId()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context is null)
        {
            return null;
        }

        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            return correlationId?.ToString();
        }

        return context.TraceIdentifier;
    }

    private string? GetHttpClientIpAddress()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context is null)
        {
            return null;
        }

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private IReadOnlyDictionary<string, string> BuildMetadata()
    {
        var metadata = new Dictionary<string, string>();

        var envelope = GetMessageEnvelope();
        if (envelope?.Headers is { Count: > 0 } headers)
        {
            foreach (var header in headers)
            {
                if (header.Value is not null)
                {
                    metadata[header.Key] = header.Value;
                }
            }
        }

        var context = _httpContextAccessor?.HttpContext;
        if (context is not null)
        {
            metadata["RequestPath"] = context.Request.Path.ToString();
            metadata["RequestMethod"] = context.Request.Method;
        }

        return metadata.Count == 0 ? EmptyMetadata : metadata;
    }

    private static string? FindClaimValue(ClaimsPrincipal user, IEnumerable<string> claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
