using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.Storage;

public sealed class AzureBlobStorage(
    BlobServiceClient blobServiceClient,
    IOptions<AzureBlobStorageOptions> options) : IBlobStorage
{
    private const string DefaultContentType = "application/octet-stream";

    private static readonly IReadOnlyDictionary<string, string> UploadHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["x-ms-blob-type"] = "BlockBlob"
    };

    private readonly AzureBlobStorageOptions _options = options.Value;
    private readonly string _keyPrefix = options.Value.ResolveKeyPrefix();

    public async Task<BlobUploadDescriptor> CreateUploadAsync(
        string blobKey,
        string contentType,
        long byteLength,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = BlobKeyValidator.NormalizeOrThrow(blobKey);
        var containerClient = await GetContainerClientAsync(cancellationToken);
        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? DefaultContentType
            : contentType.Trim();
        var blobClient = containerClient.GetBlobClient(_keyPrefix + normalizedKey);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _options.UploadUrlTtlMinutes));

        return new BlobUploadDescriptor(
            normalizedKey,
            CreateSasUri(blobClient, BlobSasPermissions.Create | BlobSasPermissions.Write, expiresAt),
            expiresAt.UtcDateTime,
            normalizedContentType,
            byteLength,
            UploadHeaders);
    }

    public async Task<BlobObjectInfo?> GetBlobAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = BlobKeyValidator.NormalizeOrThrow(blobKey);
        var containerClient = await GetContainerClientAsync(cancellationToken);
        var blobClient = containerClient.GetBlobClient(_keyPrefix + normalizedKey);
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        var sha256Hash = await ComputeSha256Async(blobClient, cancellationToken);

        return new BlobObjectInfo(
            normalizedKey,
            properties.Value.ContentType ?? DefaultContentType,
            properties.Value.ContentLength,
            sha256Hash);
    }

    public async Task<Uri> CreateDownloadUrlAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = BlobKeyValidator.NormalizeOrThrow(blobKey);
        var containerClient = await GetContainerClientAsync(cancellationToken);
        var blobClient = containerClient.GetBlobClient(_keyPrefix + normalizedKey);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _options.DownloadUrlTtlMinutes));
        return CreateSasUri(blobClient, BlobSasPermissions.Read, expiresAt);
    }

    public async Task DeleteAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        var normalizedKey = BlobKeyValidator.NormalizeOrThrow(blobKey);
        var containerClient = await GetContainerClientAsync(cancellationToken);
        await containerClient.DeleteBlobIfExistsAsync(_keyPrefix + normalizedKey, cancellationToken: cancellationToken);
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return containerClient;
    }

    private static Uri CreateSasUri(BlobClient blobClient, BlobSasPermissions permissions, DateTimeOffset expiresAt)
    {
        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("Blob SAS generation requires a shared key-based connection string.");
        }

        var builder = new BlobSasBuilder(permissions, expiresAt)
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b"
        };

        return blobClient.GenerateSasUri(builder);
    }

    private static async Task<string> ComputeSha256Async(BlobClient blobClient, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        await using var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
