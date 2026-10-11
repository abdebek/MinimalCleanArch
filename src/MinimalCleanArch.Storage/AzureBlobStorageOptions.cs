using System.ComponentModel.DataAnnotations;

namespace MinimalCleanArch.Storage;

public sealed class AzureBlobStorageOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = "app-data";

    [Range(1, 1440)]
    public int UploadUrlTtlMinutes { get; set; } = 15;

    [Range(1, 1440)]
    public int DownloadUrlTtlMinutes { get; set; } = 15;

    /// <summary>
    /// Optional key prefix (folder) prepended to every blob key, e.g. <c>uploads/</c>.
    /// Empty = container root. Bound from <c>BlobStorage:KeyPrefix</c> when using <see cref="ServiceCollectionExtensions.AddBlobStorage"/>.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Normalized key prefix: empty when unset, otherwise trimmed and ends with <c>/</c>.
    /// </summary>
    public string ResolveKeyPrefix()
    {
        var prefix = (KeyPrefix ?? string.Empty).Trim();
        return prefix.Length == 0 ? string.Empty : (prefix.EndsWith('/') ? prefix : prefix + "/");
    }
}
