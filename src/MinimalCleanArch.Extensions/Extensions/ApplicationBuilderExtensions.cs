using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Extensions.Middlewares;

namespace MinimalCleanArch.Extensions.Extensions;

/// <summary>
/// Extension methods for configuring the application pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the correlation ID middleware to the pipeline.
    /// Should be added early in the pipeline to ensure all requests have a correlation ID.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="headerName">Optional custom header name for the correlation ID.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseCorrelationId(this WebApplication app, string? headerName = null)
    {
        if (headerName != null)
        {
            app.UseMiddleware<CorrelationIdMiddleware>(headerName);
        }
        else
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
        }

        return app;
    }

    /// <summary>
    /// Adds the security headers middleware to the pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="options">Optional security headers options.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseSecurityHeaders(this WebApplication app, SecurityHeadersOptions? options = null)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>(options ?? new SecurityHeadersOptions());
        return app;
    }

    /// <summary>
    /// Adds the security headers middleware configured for API applications.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseApiSecurityHeaders(this WebApplication app)
    {
        return app.UseSecurityHeaders(SecurityHeadersOptions.ForApi());
    }

    /// <summary>
    /// Adds the global error handling middleware to the pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseGlobalErrorHandling(this WebApplication app)
    {
        app.UseMiddleware<ErrorHandlingMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds the standard MinimalCleanArch middleware pipeline.
    /// Includes correlation ID, security headers, and error handling in the correct order.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="securityOptions">Optional security headers options.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseMinimalCleanArchDefaults(
        this WebApplication app,
        SecurityHeadersOptions? securityOptions = null)
    {
        // Order matters:
        // 1. Correlation ID first so all subsequent middleware/logs have it
        app.UseCorrelationId();

        // 2. Security headers
        app.UseSecurityHeaders(securityOptions);

        // 3. Error handling wraps the rest of the pipeline
        app.UseGlobalErrorHandling();

        return app;
    }

    /// <summary>
    /// Marks the application startup as complete for health checks.
    /// Call this after all startup operations are complete.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MarkStartupComplete(this WebApplication app)
    {
        var startupHealthCheck = app.Services.GetService<HealthChecks.StartupHealthCheck>();
        startupHealthCheck?.MarkStartupComplete();
        return app;
    }
}
