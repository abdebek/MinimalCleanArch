using System.ComponentModel.DataAnnotations;

namespace MinimalCleanArch.Storage;

/// <summary>
/// Unified blob storage section: <c>BlobStorage</c>.
/// Provider selects Azure Blob (incl. Azurite) or Cloudflare R2 (S3 API).
/// </summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// <c>Azure</c> (default) or <c>R2</c>.
    /// </summary>
    public string Provider { get; set; } = BlobStorageProviders.Azure;

    // --- Azure (also maps to <see cref="AzureBlobStorageOptions"/>) ---
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = "app-data";

    [Range(1, 1440)]
    public int UploadUrlTtlMinutes { get; set; } = 15;

    [Range(1, 1440)]
    public int DownloadUrlTtlMinutes { get; set; } = 15;

    // --- Cloudflare R2 (S3-compatible) ---
    /// <summary>R2 S3 API endpoint, e.g. https://&lt;accountid&gt;.r2.cloudflarestorage.com</summary>
    public string? R2ServiceUrl { get; set; }

    public string? R2AccessKeyId { get; set; }

    public string? R2SecretAccessKey { get; set; }

    /// <summary>Bucket name (falls back to <see cref="ContainerName"/> when unset).</summary>
    public string? R2BucketName { get; set; }

    /// <summary>
    /// Optional public/custom download base. Uploads always use the S3 endpoint.
    /// </summary>
    public string? R2PublicBaseUrl { get; set; }

    /// <summary>
    /// Optional key prefix (folder) prepended to every blob key, e.g. <c>uploads/</c>.
    /// Empty = bucket/container root.
    /// </summary>
    public string? KeyPrefix { get; set; }

    public bool IsR2 =>
        string.Equals(Provider, BlobStorageProviders.R2, StringComparison.OrdinalIgnoreCase);

    public string ResolveBucketOrContainer()
        => IsR2
            ? (string.IsNullOrWhiteSpace(R2BucketName) ? ContainerName : R2BucketName)
            : ContainerName;

    /// <summary>
    /// Normalized key prefix: empty when unset, otherwise trimmed and ends with <c>/</c>.
    /// </summary>
    public string ResolveKeyPrefix()
    {
        var prefix = (KeyPrefix ?? string.Empty).Trim();
        return prefix.Length == 0 ? string.Empty : (prefix.EndsWith('/') ? prefix : prefix + "/");
    }

    public AzureBlobStorageOptions ToAzureOptions() => new()
    {
        ConnectionString = ConnectionString,
        ContainerName = ContainerName,
        UploadUrlTtlMinutes = UploadUrlTtlMinutes,
        DownloadUrlTtlMinutes = DownloadUrlTtlMinutes,
    };
}

public static class BlobStorageProviders
{
    public const string Azure = "Azure";
    public const string R2 = "R2";
}
