using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalCleanArch.Extensions.Versioning;

/// <summary>
/// Extension methods for configuring API versioning.
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Adds API versioning services with sensible defaults for Minimal APIs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure versioning options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMinimalCleanArchApiVersioning(
        this IServiceCollection services,
        Action<ApiVersioningOptions>? configure = null)
    {
        services.AddApiVersioning(options =>
        {
            // Default version when not specified
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Report versions in response headers
            options.ReportApiVersions = true;

            // Allow version to be specified via URL segment, query string, or header
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("X-Api-Version"));

            // Custom configuration
            configure?.Invoke(options);
        });

        return services;
    }

    /// <summary>
    /// Creates a versioned API route group.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="prefix">The route prefix (e.g., "api").</param>
    /// <returns>A versioned route group set.</returns>
    public static IVersionedEndpointRouteBuilder MapVersionedApi(
        this WebApplication app,
        string prefix = "api")
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(2, 0))
            .ReportApiVersions()
            .Build();

        return new VersionedEndpointRouteBuilder(app, prefix, versionSet);
    }

    /// <summary>
    /// Creates a versioned API with custom version definitions.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="prefix">The route prefix.</param>
    /// <param name="configureVersions">Action to configure available versions.</param>
    /// <returns>A versioned route group set.</returns>
    public static IVersionedEndpointRouteBuilder MapVersionedApi(
        this WebApplication app,
        string prefix,
        Action<ApiVersionSetBuilder> configureVersions)
    {
        var versionSetBuilder = app.NewApiVersionSet();
        configureVersions(versionSetBuilder);
        var versionSet = versionSetBuilder.ReportApiVersions().Build();

        return new VersionedEndpointRouteBuilder(app, prefix, versionSet);
    }
}

/// <summary>
/// Interface for building versioned endpoint routes.
/// </summary>
public interface IVersionedEndpointRouteBuilder
{
    /// <summary>
    /// Maps a route group for a specific API version.
    /// </summary>
    /// <param name="version">The API version (e.g., 1, 2).</param>
    /// <returns>A route group builder for the versioned endpoints.</returns>
    RouteGroupBuilder MapVersion(int version);

    /// <summary>
    /// Maps a route group for a specific API version with minor version.
    /// </summary>
    /// <param name="majorVersion">The major version.</param>
    /// <param name="minorVersion">The minor version.</param>
    /// <returns>A route group builder for the versioned endpoints.</returns>
    RouteGroupBuilder MapVersion(int majorVersion, int minorVersion);
}

/// <summary>
/// Default implementation of versioned endpoint route builder.
/// </summary>
public class VersionedEndpointRouteBuilder : IVersionedEndpointRouteBuilder
{
    private readonly WebApplication _app;
    private readonly string _prefix;
    private readonly ApiVersionSet _versionSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionedEndpointRouteBuilder"/> class.
    /// </summary>
    public VersionedEndpointRouteBuilder(WebApplication app, string prefix, ApiVersionSet versionSet)
    {
        _app = app;
        _prefix = prefix;
        _versionSet = versionSet;
    }

    /// <inheritdoc />
    public RouteGroupBuilder MapVersion(int version)
    {
        return _app.MapGroup($"/{_prefix}/v{version}")
            .WithApiVersionSet(_versionSet)
            .MapToApiVersion(new ApiVersion(version, 0));
    }

    /// <inheritdoc />
    public RouteGroupBuilder MapVersion(int majorVersion, int minorVersion)
    {
        return _app.MapGroup($"/{_prefix}/v{majorVersion}.{minorVersion}")
            .WithApiVersionSet(_versionSet)
            .MapToApiVersion(new ApiVersion(majorVersion, minorVersion));
    }
}

/// <summary>
/// Extension methods for route handlers to specify version requirements.
/// </summary>
public static class VersionedRouteExtensions
{
    /// <summary>
    /// Marks an endpoint as available in a specific version.
    /// </summary>
    public static RouteHandlerBuilder MapToVersion(this RouteHandlerBuilder builder, int version)
    {
        return builder.MapToApiVersion(new ApiVersion(version, 0));
    }

    /// <summary>
    /// Marks an endpoint as available in a specific version.
    /// </summary>
    public static RouteHandlerBuilder MapToVersion(this RouteHandlerBuilder builder, int major, int minor)
    {
        return builder.MapToApiVersion(new ApiVersion(major, minor));
    }
}
