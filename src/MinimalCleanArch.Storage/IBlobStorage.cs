namespace MinimalCleanArch.Storage;

public interface IBlobStorage
{
    Task<BlobUploadDescriptor> CreateUploadAsync(
        string blobKey,
        string contentType,
        long byteLength,
        CancellationToken cancellationToken = default);

    Task<BlobObjectInfo?> GetBlobAsync(string blobKey, CancellationToken cancellationToken = default);

    Task<Uri> CreateDownloadUrlAsync(string blobKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string blobKey, CancellationToken cancellationToken = default);
}

public sealed record BlobUploadDescriptor(
    string BlobKey,
    Uri UploadUrl,
    DateTime ExpiresAt,
    string ContentType,
    long ByteLength,
    IReadOnlyDictionary<string, string> RequiredHeaders);

public sealed record BlobObjectInfo(
    string BlobKey,
    string ContentType,
    long ByteLength,
    string Sha256Hash);
