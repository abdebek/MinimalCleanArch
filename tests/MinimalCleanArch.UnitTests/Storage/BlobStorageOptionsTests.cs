using FluentAssertions;
using MinimalCleanArch.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MinimalCleanArch.UnitTests.Storage;

public class BlobStorageOptionsTests
{
    [Fact]
    public void ResolveBucketOrContainer_Uses_R2Bucket_When_Provider_R2()
    {
        var settings = new BlobStorageOptions
        {
            Provider = BlobStorageProviders.R2,
            ContainerName = "app-data",
            R2BucketName = "r2-proofs",
        };

        settings.ResolveBucketOrContainer().Should().Be("r2-proofs");
    }

    [Fact]
    public void ResolveBucketOrContainer_Falls_Back_To_ContainerName_For_R2()
    {
        var settings = new BlobStorageOptions
        {
            Provider = BlobStorageProviders.R2,
            ContainerName = "app-data",
            R2BucketName = null,
        };

        settings.ResolveBucketOrContainer().Should().Be("app-data");
    }

    [Fact]
    public void ResolveKeyPrefix_Returns_Empty_For_No_Prefix()
    {
        var settings = new BlobStorageOptions { KeyPrefix = null };
        settings.ResolveKeyPrefix().Should().Be(string.Empty);
    }

    [Fact]
    public void ResolveKeyPrefix_Appends_Trailing_Slash_When_Missing()
    {
        var settings = new BlobStorageOptions { KeyPrefix = "uploads" };
        settings.ResolveKeyPrefix().Should().Be("uploads/");
    }

    [Fact]
    public void CreateClient_Requires_R2_Credentials()
    {
        var settings = new BlobStorageOptions
        {
            Provider = BlobStorageProviders.R2,
            R2ServiceUrl = null,
        };

        var act = () => R2BlobStorage.CreateClient(settings);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*R2ServiceUrl*");
    }

    [Fact]
    public void CreateClient_Builds_S3_Client_For_Valid_R2_Config()
    {
        var settings = new BlobStorageOptions
        {
            Provider = BlobStorageProviders.R2,
            R2ServiceUrl = "https://abc123.r2.cloudflarestorage.com",
            R2AccessKeyId = "key",
            R2SecretAccessKey = "secret",
            R2BucketName = "app-data",
        };

        using var client = R2BlobStorage.CreateClient(settings);
        client.Should().NotBeNull();
        client.Config.ServiceURL.Should().StartWith("https://abc123.r2.cloudflarestorage.com");
    }

    [Fact]
    public void AddBlobStorage_WithProvider_R2_Registers_R2BlobStorage()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BlobStorage:Provider"] = "R2",
                ["BlobStorage:ContainerName"] = "app-data",
                ["BlobStorage:R2ServiceUrl"] = "https://abc123.r2.cloudflarestorage.com",
                ["BlobStorage:R2AccessKeyId"] = "key",
                ["BlobStorage:R2SecretAccessKey"] = "secret",
                ["BlobStorage:R2BucketName"] = "app-data",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBlobStorage(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IBlobStorage>().Should().BeOfType<R2BlobStorage>();
    }

    [Fact]
    public void AddBlobStorage_Default_Registers_AzureBlobStorage()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["BlobStorage:ContainerName"] = "documents",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBlobStorage(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IBlobStorage>().Should().BeOfType<AzureBlobStorage>();
    }

    [Theory]
    [InlineData("https://cdn.example.com", "uploads/file.bin", "https://cdn.example.com/uploads/file.bin")]
    [InlineData("https://cdn.example.com/", "uploads/file.bin", "https://cdn.example.com/uploads/file.bin")]
    [InlineData("https://cdn.example.com/prefix", "uploads/file.bin", "https://cdn.example.com/prefix/uploads/file.bin")]
    [InlineData("https://cdn.example.com/prefix/", "file.bin", "https://cdn.example.com/prefix/file.bin")]
    public async Task CreateDownloadUrlAsync_With_R2PublicBaseUrl_Returns_Public_Url(string publicBase, string blobKey, string expected)
    {
        var settings = new BlobStorageOptions
        {
            Provider = BlobStorageProviders.R2,
            R2ServiceUrl = "https://abc123.r2.cloudflarestorage.com",
            R2AccessKeyId = "key",
            R2SecretAccessKey = "secret",
            R2BucketName = "app-data",
            R2PublicBaseUrl = publicBase,
        };
        var options = Microsoft.Extensions.Options.Options.Create(settings);

        using var storage = new R2BlobStorage(options, Microsoft.Extensions.Logging.Abstractions.NullLogger<R2BlobStorage>.Instance);

        var url = await storage.CreateDownloadUrlAsync(blobKey);

        url.ToString().Should().Be(expected);
    }

    [Fact]
    public void AzureBlobStorageOptions_ResolveKeyPrefix_Normalizes_Prefix()
    {
        new AzureBlobStorageOptions { KeyPrefix = null }.ResolveKeyPrefix().Should().Be(string.Empty);
        new AzureBlobStorageOptions { KeyPrefix = "  " }.ResolveKeyPrefix().Should().Be(string.Empty);
        new AzureBlobStorageOptions { KeyPrefix = "uploads" }.ResolveKeyPrefix().Should().Be("uploads/");
        new AzureBlobStorageOptions { KeyPrefix = "uploads/" }.ResolveKeyPrefix().Should().Be("uploads/");
    }

    [Fact]
    public void AddBlobStorage_Binds_KeyPrefix_To_AzureBlobStorageOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["BlobStorage:ContainerName"] = "app-data",
                ["BlobStorage:KeyPrefix"] = "uploads/",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddAzureBlobStorage(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
        options.KeyPrefix.Should().Be("uploads/");
        options.ResolveKeyPrefix().Should().Be("uploads/");
    }

    [Fact]
    public void AddBlobStorage_WithProvider_R2_Fails_Startup_When_Credentials_Missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BlobStorage:Provider"] = "R2",
                ["BlobStorage:ContainerName"] = "app-data",
                // R2ServiceUrl / R2AccessKeyId / R2SecretAccessKey omitted
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBlobStorage(configuration);

        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
        act.Should().Throw<OptionsValidationException>();
    }
}
