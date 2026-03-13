using FluentAssertions;
using MinimalCleanArch.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.UnitTests.Storage;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAzureBlobStorage_WithConfiguration_RegistersBlobStorageAndOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["BlobStorage:ContainerName"] = "documents",
                ["BlobStorage:UploadUrlTtlMinutes"] = "30",
                ["BlobStorage:DownloadUrlTtlMinutes"] = "45"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddAzureBlobStorage(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;

        provider.GetRequiredService<IBlobStorage>().Should().BeOfType<AzureBlobStorage>();
        options.ConnectionString.Should().Be("UseDevelopmentStorage=true");
        options.ContainerName.Should().Be("documents");
        options.UploadUrlTtlMinutes.Should().Be(30);
        options.DownloadUrlTtlMinutes.Should().Be(45);
    }

    [Fact]
    public void AddAzureBlobStorage_WithAction_RegistersBlobServiceClient()
    {
        var services = new ServiceCollection();

        services.AddAzureBlobStorage(options =>
        {
            options.ConnectionString = "UseDevelopmentStorage=true";
            options.ContainerName = "exports";
        });

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<Azure.Storage.Blobs.BlobServiceClient>().Should().NotBeNull();
        provider.GetRequiredService<IBlobStorage>().Should().BeOfType<AzureBlobStorage>();
    }
}
