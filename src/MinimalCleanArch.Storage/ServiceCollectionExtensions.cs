using Amazon.S3;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.Storage;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers blob storage from the <c>BlobStorage</c> section.
    /// Set <c>BlobStorage:Provider</c> to <c>Azure</c> (default, Azurite-compatible) or <c>R2</c> (Cloudflare).
    /// </summary>
    public static IServiceCollection AddBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = BlobStorageOptions.SectionName)
    {
        services
            .AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart<BlobStorageOptions>();

        var provider = configuration.GetSection(sectionName)["Provider"] ?? BlobStorageProviders.Azure;
        if (string.Equals(provider, BlobStorageProviders.R2, StringComparison.OrdinalIgnoreCase))
        {
            services
                .AddOptions<BlobStorageOptions>()
                .Configure(o => o.Provider = BlobStorageProviders.R2)
                .Validate(R2BlobStorage.ValidateOptions, "BlobStorage Provider=R2 requires R2ServiceUrl, R2AccessKeyId, and R2SecretAccessKey.");
            return services.AddR2BlobStorageCore();
        }

        // Azure path: also bind Azure-specific options for AzureBlobStorage.
        services
            .AddOptions<AzureBlobStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart<AzureBlobStorageOptions>();

        return services.AddAzureBlobStorageCore();
    }

    /// <summary>
    /// Registers Azure Blob Storage (production Azure or local Azurite).
    /// Prefer <see cref="AddBlobStorage"/> when Provider switching is desired.
    /// </summary>
    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "BlobStorage")
    {
        services
            .AddOptions<AzureBlobStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart<AzureBlobStorageOptions>();

        services
            .AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Configure(o => o.Provider = BlobStorageProviders.Azure);

        return services.AddAzureBlobStorageCore();
    }

    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        Action<AzureBlobStorageOptions> configureOptions)
    {
        services
            .AddOptions<AzureBlobStorageOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart<AzureBlobStorageOptions>();

        services.Configure<BlobStorageOptions>(o => o.Provider = BlobStorageProviders.Azure);

        return services.AddAzureBlobStorageCore();
    }

    /// <summary>
    /// Registers Cloudflare R2 (S3 API) blob storage from configuration.
    /// </summary>
    public static IServiceCollection AddR2BlobStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = BlobStorageOptions.SectionName)
    {
        services
            .AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Configure(o => o.Provider = BlobStorageProviders.R2)
            .ValidateDataAnnotations()
            .Validate(R2BlobStorage.ValidateOptions, "BlobStorage Provider=R2 requires R2ServiceUrl, R2AccessKeyId, and R2SecretAccessKey.")
            .ValidateOnStart<BlobStorageOptions>();

        return services.AddR2BlobStorageCore();
    }

    public static IServiceCollection AddR2BlobStorage(
        this IServiceCollection services,
        Action<BlobStorageOptions> configureOptions)
    {
        services
            .AddOptions<BlobStorageOptions>()
            .Configure(configureOptions)
            .Configure(o => o.Provider = BlobStorageProviders.R2)
            .ValidateDataAnnotations()
            .Validate(R2BlobStorage.ValidateOptions, "BlobStorage Provider=R2 requires R2ServiceUrl, R2AccessKeyId, and R2SecretAccessKey.")
            .ValidateOnStart<BlobStorageOptions>();

        return services.AddR2BlobStorageCore();
    }

    private static IServiceCollection AddAzureBlobStorageCore(this IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
            return new BlobServiceClient(options.ConnectionString);
        });
        services.TryAddSingleton<AzureBlobStorage>();
        services.TryAddSingleton<IBlobStorage>(sp => sp.GetRequiredService<AzureBlobStorage>());
        return services;
    }

    private static IServiceCollection AddR2BlobStorageCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IAmazonS3>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            return R2BlobStorage.CreateClient(settings);
        });
        services.TryAddSingleton<IBlobStorage>(sp =>
            new R2BlobStorage(
                sp.GetRequiredService<IAmazonS3>(),
                sp.GetRequiredService<IOptions<BlobStorageOptions>>(),
                sp.GetRequiredService<ILogger<R2BlobStorage>>()));
        return services;
    }
}
