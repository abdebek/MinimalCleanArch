using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace MinimalCleanArch.Extensions.Logging;

/// <summary>
/// Extension methods for configuring Serilog structured logging.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider with sensible defaults for production use.
    /// Includes console output, file logging with rolling, and enrichment with machine/thread info.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configure">Optional action to further configure the logger.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddSerilogLogging(
        this WebApplicationBuilder builder,
        Action<LoggerConfiguration>? configure = null)
    {
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);

        // Default minimum level based on environment
        if (builder.Environment.IsDevelopment())
        {
            loggerConfig.MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}" +
                    "      {Message:lj}{NewLine}" +
                    "      {Properties:j}{NewLine}" +
                    "{Exception}");
        }
        else
        {
            loggerConfig.MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .WriteTo.Console(new CompactJsonFormatter());
        }

        // File logging with rolling
        var logPath = builder.Configuration["Logging:FilePath"] ?? "logs/log-.txt";
        loggerConfig.WriteTo.File(
            new CompactJsonFormatter(),
            logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 100_000_000, // 100MB
            rollOnFileSizeLimit: true);

        // Allow additional configuration
        configure?.Invoke(loggerConfig);

        Log.Logger = loggerConfig.CreateLogger();
        builder.Host.UseSerilog();

        return builder;
    }

    /// <summary>
    /// Adds Serilog request logging middleware with performance metrics.
    /// Logs HTTP request information including method, path, status code, and elapsed time.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseSerilogRequestLogging(this WebApplication app)
    {
        SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(app, options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value ??
                                 httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (userId != null)
                    {
                        diagnosticContext.Set("UserId", userId);
                    }
                }
            };

            // Customize the log level based on status code
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null || httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                if (elapsed > 3000) // Slow requests > 3s
                    return LogEventLevel.Warning;

                return LogEventLevel.Information;
            };
        });

        return app;
    }
}
