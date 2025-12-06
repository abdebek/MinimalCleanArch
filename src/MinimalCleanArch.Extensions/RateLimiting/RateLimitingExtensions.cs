using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalCleanArch.Extensions.RateLimiting;

/// <summary>
/// Extension methods for configuring rate limiting.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Policy name for the fixed window rate limiter.
    /// </summary>
    public const string FixedPolicy = "fixed";

    /// <summary>
    /// Policy name for the sliding window rate limiter.
    /// </summary>
    public const string SlidingPolicy = "sliding";

    /// <summary>
    /// Policy name for the token bucket rate limiter.
    /// </summary>
    public const string TokenBucketPolicy = "token";

    /// <summary>
    /// Policy name for the concurrency limiter.
    /// </summary>
    public const string ConcurrencyPolicy = "concurrency";

    /// <summary>
    /// Adds rate limiting services with common pre-configured policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure rate limiting options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMinimalCleanArchRateLimiting(
        this IServiceCollection services,
        Action<RateLimitingConfiguration>? configure = null)
    {
        var config = new RateLimitingConfiguration();
        configure?.Invoke(config);

        services.AddRateLimiter(options =>
        {
            // Global limiter applies to all requests
            if (config.EnableGlobalLimiter)
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var clientId = GetClientIdentifier(context, config);
                    return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = config.GlobalPermitLimit,
                        Window = config.GlobalWindow,
                        QueueLimit = config.GlobalQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
                });
            }

            // Fixed window policy
            options.AddPolicy(FixedPolicy, context =>
            {
                var clientId = GetClientIdentifier(context, config);
                return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = config.FixedPermitLimit,
                    Window = config.FixedWindow,
                    QueueLimit = config.FixedQueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            // Sliding window policy
            options.AddPolicy(SlidingPolicy, context =>
            {
                var clientId = GetClientIdentifier(context, config);
                return RateLimitPartition.GetSlidingWindowLimiter(clientId, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = config.SlidingPermitLimit,
                    Window = config.SlidingWindow,
                    SegmentsPerWindow = config.SlidingSegmentsPerWindow,
                    QueueLimit = config.SlidingQueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            // Token bucket policy
            options.AddPolicy(TokenBucketPolicy, context =>
            {
                var clientId = GetClientIdentifier(context, config);
                return RateLimitPartition.GetTokenBucketLimiter(clientId, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = config.TokenBucketLimit,
                    ReplenishmentPeriod = config.TokenReplenishmentPeriod,
                    TokensPerPeriod = config.TokensPerPeriod,
                    QueueLimit = config.TokenQueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            // Concurrency policy
            options.AddPolicy(ConcurrencyPolicy, context =>
            {
                var clientId = GetClientIdentifier(context, config);
                return RateLimitPartition.GetConcurrencyLimiter(clientId, _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = config.ConcurrencyPermitLimit,
                    QueueLimit = config.ConcurrencyQueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            // Rejection response
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";

                var retryAfter = GetRetryAfterSeconds(context);
                if (retryAfter.HasValue)
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.Value.ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/429",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Please try again later.",
                    retryAfter = retryAfter
                }, cancellationToken: token);
            };
        });

        return services;
    }

    /// <summary>
    /// Adds the rate limiting middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseMinimalCleanArchRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }

    private static string GetClientIdentifier(HttpContext context, RateLimitingConfiguration config)
    {
        // Prefer authenticated user ID
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value ??
                        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        // Fall back to client IP
        var clientIp = context.Connection.RemoteIpAddress?.ToString();

        // Check for forwarded headers
        if (config.UseForwardedHeaders)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                clientIp = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }
        }

        return $"ip:{clientIp ?? "unknown"}";
    }

    private static int? GetRetryAfterSeconds(OnRejectedContext context)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return (int)retryAfter.TotalSeconds;
        }
        return null;
    }
}

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public class RateLimitingConfiguration
{
    /// <summary>
    /// Gets or sets whether to enable the global rate limiter. Default: true.
    /// </summary>
    public bool EnableGlobalLimiter { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use X-Forwarded-For headers for client identification. Default: true.
    /// </summary>
    public bool UseForwardedHeaders { get; set; } = true;

    // Global limiter settings
    /// <summary>
    /// Gets or sets the global permit limit per window. Default: 1000.
    /// </summary>
    public int GlobalPermitLimit { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the global rate limit window. Default: 1 minute.
    /// </summary>
    public TimeSpan GlobalWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the global queue limit. Default: 0 (no queuing).
    /// </summary>
    public int GlobalQueueLimit { get; set; } = 0;

    // Fixed window settings
    /// <summary>
    /// Gets or sets the fixed window permit limit. Default: 100.
    /// </summary>
    public int FixedPermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the fixed window duration. Default: 1 minute.
    /// </summary>
    public TimeSpan FixedWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the fixed window queue limit. Default: 0.
    /// </summary>
    public int FixedQueueLimit { get; set; } = 0;

    // Sliding window settings
    /// <summary>
    /// Gets or sets the sliding window permit limit. Default: 100.
    /// </summary>
    public int SlidingPermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the sliding window duration. Default: 1 minute.
    /// </summary>
    public TimeSpan SlidingWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the number of segments per sliding window. Default: 4.
    /// </summary>
    public int SlidingSegmentsPerWindow { get; set; } = 4;

    /// <summary>
    /// Gets or sets the sliding window queue limit. Default: 0.
    /// </summary>
    public int SlidingQueueLimit { get; set; } = 0;

    // Token bucket settings
    /// <summary>
    /// Gets or sets the token bucket limit. Default: 100.
    /// </summary>
    public int TokenBucketLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the token replenishment period. Default: 10 seconds.
    /// </summary>
    public TimeSpan TokenReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the number of tokens added per replenishment period. Default: 10.
    /// </summary>
    public int TokensPerPeriod { get; set; } = 10;

    /// <summary>
    /// Gets or sets the token bucket queue limit. Default: 0.
    /// </summary>
    public int TokenQueueLimit { get; set; } = 0;

    // Concurrency settings
    /// <summary>
    /// Gets or sets the concurrency permit limit. Default: 10.
    /// </summary>
    public int ConcurrencyPermitLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the concurrency queue limit. Default: 5.
    /// </summary>
    public int ConcurrencyQueueLimit { get; set; } = 5;

    /// <summary>
    /// Creates a configuration for strict rate limiting (low limits).
    /// </summary>
    public static RateLimitingConfiguration Strict() => new()
    {
        GlobalPermitLimit = 100,
        GlobalWindow = TimeSpan.FromMinutes(1),
        FixedPermitLimit = 20,
        FixedWindow = TimeSpan.FromMinutes(1),
        TokenBucketLimit = 20,
        TokensPerPeriod = 5,
        ConcurrencyPermitLimit = 5
    };

    /// <summary>
    /// Creates a configuration for relaxed rate limiting (high limits).
    /// </summary>
    public static RateLimitingConfiguration Relaxed() => new()
    {
        GlobalPermitLimit = 10000,
        GlobalWindow = TimeSpan.FromMinutes(1),
        FixedPermitLimit = 1000,
        FixedWindow = TimeSpan.FromMinutes(1),
        TokenBucketLimit = 500,
        TokensPerPeriod = 50,
        ConcurrencyPermitLimit = 50
    };
}
