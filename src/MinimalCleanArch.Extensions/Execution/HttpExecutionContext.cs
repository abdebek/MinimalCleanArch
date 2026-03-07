using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MinimalCleanArch.Execution;

namespace MinimalCleanArch.Extensions.Execution;

internal sealed class HttpExecutionContext : IExecutionContext
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>();

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ExecutionContextOptions _options;
    private IReadOnlyDictionary<string, string>? _metadata;

    public HttpExecutionContext(
        IHttpContextAccessor httpContextAccessor,
        IOptions<ExecutionContextOptions>? options = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options?.Value ?? new ExecutionContextOptions();
    }

    public string? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return FindClaimValue(user, _options.UserIdClaimTypes);
        }
    }

    public string? UserName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return FindClaimValue(user, _options.UserNameClaimTypes)
                ?? user.Identity?.Name
                ?? FindClaimValue(user, _options.UserNameFallbackClaimTypes);
        }
    }

    public string? TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return FindClaimValue(user, _options.TenantIdClaimTypes);
        }
    }

    public string? CorrelationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
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
    }

    public string? ClientIpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
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
    }

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public IReadOnlyDictionary<string, string> Metadata
    {
        get => _metadata ??= BuildMetadata();
    }

    private IReadOnlyDictionary<string, string> BuildMetadata()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return EmptyMetadata;
        }

        return new Dictionary<string, string>
        {
            ["RequestPath"] = context.Request.Path.ToString(),
            ["RequestMethod"] = context.Request.Method
        };
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
