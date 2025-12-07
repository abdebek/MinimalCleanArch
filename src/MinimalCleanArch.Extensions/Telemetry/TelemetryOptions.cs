namespace MinimalCleanArch.Extensions.Telemetry;

/// <summary>
/// Configuration options for OpenTelemetry integration.
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// Gets or sets the service name. Default: Assembly name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version. Default: Assembly version.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets whether to enable tracing. Default: true.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable metrics. Default: true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable console exporter (for development). Default: false.
    /// </summary>
    public bool EnableConsoleExporter { get; set; }

    /// <summary>
    /// Gets or sets whether to enable OTLP exporter. Default: true.
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = true;

    /// <summary>
    /// Gets or sets the OTLP endpoint. Default: http://localhost:4317.
    /// </summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets whether to instrument ASP.NET Core. Default: true.
    /// </summary>
    public bool InstrumentAspNetCore { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to instrument HTTP client. Default: true.
    /// </summary>
    public bool InstrumentHttpClient { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to collect runtime metrics. Default: true.
    /// </summary>
    public bool CollectRuntimeMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record exceptions in traces. Default: true.
    /// </summary>
    public bool RecordException { get; set; } = true;

    /// <summary>
    /// Gets or sets paths to exclude from tracing (e.g., health checks).
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/health/ready",
        "/health/live",
        "/health/startup",
        "/favicon.ico"
    };

    /// <summary>
    /// Gets or sets additional resource attributes.
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();
}
