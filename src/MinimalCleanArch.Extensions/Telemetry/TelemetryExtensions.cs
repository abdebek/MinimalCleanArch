using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MinimalCleanArch.Extensions.Telemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry observability.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry observability (tracing and metrics) to the application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional action to configure telemetry options.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddMinimalCleanArchTelemetry(
        this IHostApplicationBuilder builder,
        Action<TelemetryOptions>? configure = null)
    {
        var options = new TelemetryOptions();
        configure?.Invoke(options);

        // Get service name and version from assembly if not specified
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var serviceName = options.ServiceName ?? assembly.GetName().Name ?? "unknown";
        var serviceVersion = options.ServiceVersion ?? assembly.GetName().Version?.ToString() ?? "1.0.0";

        // Store options for later use
        builder.Services.AddSingleton(options);

        // Build the resource
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion)
            .AddTelemetrySdk()
            .AddAttributes(options.ResourceAttributes.Select(kv =>
                new KeyValuePair<string, object>(kv.Key, kv.Value)));

        // Configure OpenTelemetry
        var otelBuilder = builder.Services.AddOpenTelemetry();

        // Configure tracing
        if (options.EnableTracing)
        {
            otelBuilder.WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resourceBuilder);

                // Add ASP.NET Core instrumentation
                if (options.InstrumentAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation(aspnet =>
                    {
                        aspnet.RecordException = options.RecordException;
                        aspnet.Filter = ctx => !ShouldExcludePath(ctx.Request.Path, options.ExcludedPaths);
                    });
                }

                // Add HTTP client instrumentation
                if (options.InstrumentHttpClient)
                {
                    tracing.AddHttpClientInstrumentation(http =>
                    {
                        http.RecordException = options.RecordException;
                    });
                }

                // Add custom activity source for application activities
                tracing.AddSource(serviceName);

                // Add exporters
                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }

                if (options.EnableOtlpExporter && !string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                        otlp.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            });
        }

        // Configure metrics
        if (options.EnableMetrics)
        {
            otelBuilder.WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resourceBuilder);

                // Add ASP.NET Core instrumentation
                if (options.InstrumentAspNetCore)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                // Add HTTP client instrumentation
                if (options.InstrumentHttpClient)
                {
                    metrics.AddHttpClientInstrumentation();
                }

                // Add runtime metrics
                if (options.CollectRuntimeMetrics)
                {
                    metrics.AddRuntimeInstrumentation();
                }

                // Add custom meter for application metrics
                metrics.AddMeter(serviceName);

                // Add exporters
                if (options.EnableConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }

                if (options.EnableOtlpExporter && !string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(options.OtlpEndpoint);
                        otlp.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
            });
        }

        return builder;
    }

    /// <summary>
    /// Adds OpenTelemetry observability using WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configure">Optional action to configure telemetry options.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddMinimalCleanArchTelemetry(
        this WebApplicationBuilder builder,
        Action<TelemetryOptions>? configure = null)
    {
        ((IHostApplicationBuilder)builder).AddMinimalCleanArchTelemetry(configure);
        return builder;
    }

    private static bool ShouldExcludePath(PathString path, List<string> excludedPaths)
    {
        return excludedPaths.Any(excluded =>
            path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Helper class for creating custom traces and spans.
/// </summary>
public static class TelemetryActivitySource
{
    private static ActivitySource? _activitySource;

    /// <summary>
    /// Initializes the activity source with the specified name.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    public static void Initialize(string serviceName)
    {
        _activitySource = new ActivitySource(serviceName);
    }

    /// <summary>
    /// Gets the activity source for creating custom spans.
    /// </summary>
    public static ActivitySource Source => _activitySource
        ?? throw new InvalidOperationException("TelemetryActivitySource has not been initialized. Call Initialize() first.");

    /// <summary>
    /// Starts a new activity with the specified name.
    /// </summary>
    /// <param name="name">The activity name.</param>
    /// <param name="kind">The activity kind. Default: Internal.</param>
    /// <returns>The started activity, or null if no listener is registered.</returns>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource?.StartActivity(name, kind);
    }

    /// <summary>
    /// Starts a new activity with tags.
    /// </summary>
    /// <param name="name">The activity name.</param>
    /// <param name="tags">The activity tags.</param>
    /// <param name="kind">The activity kind. Default: Internal.</param>
    /// <returns>The started activity, or null if no listener is registered.</returns>
    public static Activity? StartActivity(
        string name,
        IEnumerable<KeyValuePair<string, object?>> tags,
        ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource?.StartActivity(name, kind, default(ActivityContext), tags);
    }
}

/// <summary>
/// Extension methods for adding telemetry to activities.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Records an exception on the activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="exception">The exception to record.</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity? RecordException(this Activity? activity, Exception exception)
    {
        if (activity == null) return null;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("exception.type", exception.GetType().FullName);
        activity.SetTag("exception.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.StackTrace);

        return activity;
    }

    /// <summary>
    /// Sets the activity status to OK.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity? SetSuccess(this Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
        return activity;
    }

    /// <summary>
    /// Sets the activity status to Error with a message.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="message">The error message.</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity? SetError(this Activity? activity, string message)
    {
        activity?.SetStatus(ActivityStatusCode.Error, message);
        return activity;
    }

    /// <summary>
    /// Adds an event to the activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="name">The event name.</param>
    /// <param name="attributes">Optional event attributes.</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity? AddActivityEvent(
        this Activity? activity,
        string name,
        IEnumerable<KeyValuePair<string, object?>>? attributes = null)
    {
        if (activity == null) return null;

        var tags = attributes != null
            ? new ActivityTagsCollection(attributes)
            : null;

        activity.AddEvent(new ActivityEvent(name, tags: tags));
        return activity;
    }
}
