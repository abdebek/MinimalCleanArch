using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MinimalCleanArch.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "BlobStorage")
    {
        services
            .AddOptions<AzureBlobStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

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
            .ValidateOnStart();

        return services.AddAzureBlobStorageCore();
    }

    private static IServiceCollection AddAzureBlobStorageCore(this IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AzureBlobStorageOptions>>().Value;
            return new BlobServiceClient(options.ConnectionString);
        });
        services.TryAddSingleton<AzureBlobStorage>();
        services.TryAddSingleton<IBlobStorage>(sp => sp.GetRequiredService<AzureBlobStorage>());
        return services;
    }
}
