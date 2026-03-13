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
}
