using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.Extensions.Configuration;

/// <summary>
/// Extension methods for strongly-typed configuration with validation.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds and configures a strongly-typed options class with validation.
    /// The options are bound from configuration section matching the type name (without "Options" or "Settings" suffix).
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">Optional custom section name. If null, derives from type name.</param>
    /// <returns>The options builder for additional configuration.</returns>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
        where TOptions : class
    {
        var section = sectionName ?? GetDefaultSectionName<TOptions>();

        return services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Adds and configures a strongly-typed options class with custom validation.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="validate">Validation function that returns true if valid.</param>
    /// <param name="failureMessage">Message to display on validation failure.</param>
    /// <param name="sectionName">Optional custom section name.</param>
    /// <returns>The options builder for additional configuration.</returns>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        Func<TOptions, bool> validate,
        string failureMessage,
        string? sectionName = null)
        where TOptions : class
    {
        var section = sectionName ?? GetDefaultSectionName<TOptions>();

        return services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(section))
            .ValidateDataAnnotations()
            .Validate(validate, failureMessage)
            .ValidateOnStart();
    }

    /// <summary>
    /// Adds and configures a strongly-typed options class with IValidateOptions validator.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">Optional custom section name.</param>
    /// <returns>The options builder for additional configuration.</returns>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions, TValidator>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();

        var section = sectionName ?? GetDefaultSectionName<TOptions>();

        return services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Gets a required configuration section, throwing if not found or empty.
    /// </summary>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="key">The configuration section key.</param>
    /// <returns>The configuration section.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the section is missing or empty.</exception>
    public static IConfigurationSection GetRequiredSection(this IConfiguration configuration, string key)
    {
        var section = configuration.GetSection(key);

        if (!section.Exists() || !section.GetChildren().Any())
        {
            throw new InvalidOperationException($"Configuration section '{key}' is required but was not found or is empty.");
        }

        return section;
    }

    /// <summary>
    /// Gets a required configuration value, throwing if not found.
    /// </summary>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the value is missing.</exception>
    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{key}' is required but was not found or is empty.");
        }

        return value;
    }

    /// <summary>
    /// Gets a configuration value or returns a default.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">Default value if not found.</param>
    /// <returns>The configuration value or default.</returns>
    public static T GetValueOrDefault<T>(this IConfiguration configuration, string key, T defaultValue)
    {
        var value = configuration.GetValue<T>(key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Binds and returns a configuration section to a new instance.
    /// </summary>
    /// <typeparam name="T">The type to bind to.</typeparam>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">The section name. If null, derives from type name.</param>
    /// <returns>A new instance bound from configuration.</returns>
    public static T BindSection<T>(this IConfiguration configuration, string? sectionName = null)
        where T : class, new()
    {
        var section = sectionName ?? GetDefaultSectionName<T>();
        var instance = new T();
        configuration.GetSection(section).Bind(instance);
        return instance;
    }

    private static string GetDefaultSectionName<T>()
    {
        var typeName = typeof(T).Name;

        // Remove common suffixes
        if (typeName.EndsWith("Options", StringComparison.OrdinalIgnoreCase))
        {
            return typeName[..^7];
        }

        if (typeName.EndsWith("Settings", StringComparison.OrdinalIgnoreCase))
        {
            return typeName[..^8];
        }

        if (typeName.EndsWith("Configuration", StringComparison.OrdinalIgnoreCase))
        {
            return typeName[..^13];
        }

        return typeName;
    }
}

/// <summary>
/// Base class for configuration options with validation support.
/// </summary>
public abstract class ValidatableOptions
{
    /// <summary>
    /// Validates the options and returns any validation errors.
    /// </summary>
    /// <returns>Collection of validation errors, empty if valid.</returns>
    public virtual IEnumerable<string> Validate()
    {
        return Enumerable.Empty<string>();
    }
}
