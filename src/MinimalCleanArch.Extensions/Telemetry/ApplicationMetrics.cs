using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MinimalCleanArch.Extensions.Telemetry;

/// <summary>
/// Helper class for creating and recording application-specific metrics.
/// </summary>
public class ApplicationMetrics : IDisposable
{
    private readonly Meter _meter;
    private bool _disposed;

    /// <summary>
    /// Gets the underlying meter.
    /// </summary>
    public Meter Meter => _meter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMetrics"/> class.
    /// </summary>
    /// <param name="meterName">The meter name (typically the service name).</param>
    /// <param name="version">The meter version.</param>
    public ApplicationMetrics(string meterName, string? version = null)
    {
        _meter = new Meter(meterName, version);
    }

    /// <summary>
    /// Creates a counter metric.
    /// </summary>
    /// <typeparam name="T">The counter value type.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>The counter instrument.</returns>
    public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateCounter<T>(name, unit, description);
    }

    /// <summary>
    /// Creates an up-down counter metric.
    /// </summary>
    /// <typeparam name="T">The counter value type.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>The up-down counter instrument.</returns>
    public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateUpDownCounter<T>(name, unit, description);
    }

    /// <summary>
    /// Creates a histogram metric.
    /// </summary>
    /// <typeparam name="T">The histogram value type.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>The histogram instrument.</returns>
    public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateHistogram<T>(name, unit, description);
    }

    /// <summary>
    /// Creates an observable gauge metric.
    /// </summary>
    /// <typeparam name="T">The gauge value type.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="observeValue">The callback to observe the current value.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>The observable gauge instrument.</returns>
    public ObservableGauge<T> CreateObservableGauge<T>(
        string name,
        Func<T> observeValue,
        string? unit = null,
        string? description = null)
        where T : struct
    {
        return _meter.CreateObservableGauge(name, observeValue, unit, description);
    }

    /// <summary>
    /// Creates an observable counter metric.
    /// </summary>
    /// <typeparam name="T">The counter value type.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="observeValue">The callback to observe the current value.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>The observable counter instrument.</returns>
    public ObservableCounter<T> CreateObservableCounter<T>(
        string name,
        Func<T> observeValue,
        string? unit = null,
        string? description = null)
        where T : struct
    {
        return _meter.CreateObservableCounter(name, observeValue, unit, description);
    }

    /// <summary>
    /// Disposes the meter and its resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _meter.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Pre-built common metrics for web applications.
/// </summary>
public class WebApplicationMetrics : IDisposable
{
    private readonly ApplicationMetrics _metrics;
    private bool _disposed;

    /// <summary>
    /// Counter for total HTTP requests.
    /// </summary>
    public Counter<long> HttpRequestsTotal { get; }

    /// <summary>
    /// Histogram for HTTP request duration.
    /// </summary>
    public Histogram<double> HttpRequestDuration { get; }

    /// <summary>
    /// Counter for HTTP request errors.
    /// </summary>
    public Counter<long> HttpRequestErrors { get; }

    /// <summary>
    /// Counter for active connections.
    /// </summary>
    public UpDownCounter<long> ActiveConnections { get; }

    /// <summary>
    /// Counter for business operations.
    /// </summary>
    public Counter<long> BusinessOperations { get; }

    /// <summary>
    /// Histogram for business operation duration.
    /// </summary>
    public Histogram<double> BusinessOperationDuration { get; }

    /// <summary>
    /// Counter for cache hits.
    /// </summary>
    public Counter<long> CacheHits { get; }

    /// <summary>
    /// Counter for cache misses.
    /// </summary>
    public Counter<long> CacheMisses { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApplicationMetrics"/> class.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    public WebApplicationMetrics(string serviceName)
    {
        _metrics = new ApplicationMetrics(serviceName);

        HttpRequestsTotal = _metrics.CreateCounter<long>(
            "http.requests.total",
            "{requests}",
            "Total number of HTTP requests");

        HttpRequestDuration = _metrics.CreateHistogram<double>(
            "http.request.duration",
            "ms",
            "HTTP request duration in milliseconds");

        HttpRequestErrors = _metrics.CreateCounter<long>(
            "http.requests.errors",
            "{errors}",
            "Total number of HTTP request errors");

        ActiveConnections = _metrics.CreateUpDownCounter<long>(
            "http.connections.active",
            "{connections}",
            "Number of active HTTP connections");

        BusinessOperations = _metrics.CreateCounter<long>(
            "business.operations.total",
            "{operations}",
            "Total number of business operations");

        BusinessOperationDuration = _metrics.CreateHistogram<double>(
            "business.operation.duration",
            "ms",
            "Business operation duration in milliseconds");

        CacheHits = _metrics.CreateCounter<long>(
            "cache.hits",
            "{hits}",
            "Total number of cache hits");

        CacheMisses = _metrics.CreateCounter<long>(
            "cache.misses",
            "{misses}",
            "Total number of cache misses");
    }

    /// <summary>
    /// Records an HTTP request.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The request path.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    public void RecordHttpRequest(string method, string path, int statusCode, double durationMs)
    {
        var tags = new TagList
        {
            { "http.method", method },
            { "http.route", path },
            { "http.status_code", statusCode }
        };

        HttpRequestsTotal.Add(1, tags);
        HttpRequestDuration.Record(durationMs, tags);

        if (statusCode >= 400)
        {
            HttpRequestErrors.Add(1, tags);
        }
    }

    /// <summary>
    /// Records a business operation.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void RecordBusinessOperation(string operationName, bool success, double durationMs)
    {
        var tags = new TagList
        {
            { "operation.name", operationName },
            { "operation.success", success }
        };

        BusinessOperations.Add(1, tags);
        BusinessOperationDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a cache access.
    /// </summary>
    /// <param name="cacheType">The cache type (e.g., "memory", "distributed").</param>
    /// <param name="hit">Whether it was a cache hit.</param>
    public void RecordCacheAccess(string cacheType, bool hit)
    {
        var tags = new TagList { { "cache.type", cacheType } };

        if (hit)
        {
            CacheHits.Add(1, tags);
        }
        else
        {
            CacheMisses.Add(1, tags);
        }
    }

    /// <summary>
    /// Disposes the metrics and its resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _metrics.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
