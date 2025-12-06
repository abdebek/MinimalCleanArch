using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MinimalCleanArch.Extensions.HealthChecks;

/// <summary>
/// Extension methods for configuring health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds health check services with common configurations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure health check builder.</param>
    /// <returns>The health checks builder for adding custom health checks.</returns>
    public static IHealthChecksBuilder AddMinimalCleanArchHealthChecks(
        this IServiceCollection services,
        Action<IHealthChecksBuilder>? configure = null)
    {
        var builder = services.AddHealthChecks()
            .AddCheck<StartupHealthCheck>("startup", tags: ["startup"])
            .AddCheck<LivenessHealthCheck>("liveness", tags: ["liveness"])
            .AddCheck<MemoryHealthCheck>("memory", tags: ["readiness"]);

        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    /// Maps health check endpoints with different levels of detail.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="basePath">Base path for health endpoints. Default: "/health".</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapMinimalCleanArchHealthChecks(
        this WebApplication app,
        string basePath = "/health")
    {
        // Simple health check - returns 200 OK or 503 Service Unavailable
        app.MapHealthChecks(basePath, new HealthCheckOptions
        {
            ResponseWriter = WriteSimpleResponse,
            AllowCachingResponses = false
        }).WithTags("Health");

        // Detailed health check with full status
        app.MapHealthChecks($"{basePath}/detailed", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedResponse,
            AllowCachingResponses = false
        }).WithTags("Health");

        // Liveness probe - is the application alive?
        app.MapHealthChecks($"{basePath}/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("liveness"),
            ResponseWriter = WriteSimpleResponse,
            AllowCachingResponses = false
        }).WithTags("Health");

        // Readiness probe - is the application ready to receive traffic?
        app.MapHealthChecks($"{basePath}/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("readiness") || check.Tags.Contains("database"),
            ResponseWriter = WriteSimpleResponse,
            AllowCachingResponses = false
        }).WithTags("Health");

        // Startup probe - has the application started?
        app.MapHealthChecks($"{basePath}/startup", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("startup"),
            ResponseWriter = WriteSimpleResponse,
            AllowCachingResponses = false
        }).WithTags("Health");

        return app;
    }

    private static async Task WriteSimpleResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static async Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data.Count > 0 ? entry.Value.Data : null
            })
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsJsonAsync(response, options);
    }
}

/// <summary>
/// Health check that verifies the application has completed startup.
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isStartupComplete = false;

    /// <summary>
    /// Marks startup as complete.
    /// </summary>
    public void MarkStartupComplete() => _isStartupComplete = true;

    /// <summary>
    /// Checks if startup is complete.
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_isStartupComplete
            ? HealthCheckResult.Healthy("Application startup complete")
            : HealthCheckResult.Unhealthy("Application is still starting"));
    }
}

/// <summary>
/// Simple liveness health check that always returns healthy.
/// Use this to verify the application process is running.
/// </summary>
public class LivenessHealthCheck : IHealthCheck
{
    /// <summary>
    /// Always returns healthy if the application is running.
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Application is alive"));
    }
}

/// <summary>
/// Health check that monitors memory usage.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _thresholdBytes;

    /// <summary>
    /// Initializes a new instance with a memory threshold.
    /// </summary>
    /// <param name="thresholdMegabytes">Memory threshold in megabytes. Default: 1024 MB.</param>
    public MemoryHealthCheck(long thresholdMegabytes = 1024)
    {
        _thresholdBytes = thresholdMegabytes * 1024 * 1024;
    }

    /// <summary>
    /// Checks if memory usage is within acceptable limits.
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocated = GC.GetTotalMemory(forceFullCollection: false);
        var data = new Dictionary<string, object>
        {
            { "allocatedBytes", allocated },
            { "allocatedMB", allocated / 1024 / 1024 },
            { "thresholdMB", _thresholdBytes / 1024 / 1024 },
            { "gen0Collections", GC.CollectionCount(0) },
            { "gen1Collections", GC.CollectionCount(1) },
            { "gen2Collections", GC.CollectionCount(2) }
        };

        if (allocated >= _thresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Memory usage ({allocated / 1024 / 1024} MB) is above threshold ({_thresholdBytes / 1024 / 1024} MB)",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory usage is {allocated / 1024 / 1024} MB",
            data));
    }
}
