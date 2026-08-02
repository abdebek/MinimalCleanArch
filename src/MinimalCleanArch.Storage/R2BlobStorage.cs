using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.Storage;

/// <summary>
/// Cloudflare R2 implementation of <see cref="IBlobStorage"/> via the S3 API (SigV4 presigned PUT/GET).
/// See https://developers.cloudflare.com/r2/examples/aws/aws-sdk-net/
/// </summary>
public sealed class R2BlobStorage : IBlobStorage, IDisposable
{
    private readonly IAmazonS3 _s3;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<R2BlobStorage> _logger;
    private readonly string _bucket;
    private readonly string _keyPrefix;

    public R2BlobStorage(
        IOptions<BlobStorageOptions> options,
        ILogger<R2BlobStorage> logger)
        : this(CreateClient(options.Value), options, logger)
    {
    }

    internal R2BlobStorage(
        IAmazonS3 s3,
        IOptions<BlobStorageOptions> options,
        ILogger<R2BlobStorage> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _logger = logger;
        _bucket = _options.ResolveBucketOrContainer();
        if (string.IsNullOrWhiteSpace(_bucket))
        {
            throw new InvalidOperationException("BlobStorage R2 bucket/container name is required.");
        }

        _keyPrefix = _options.ResolveKeyPrefix();
    }

    private string StorageKey(string blobKey) => _keyPrefix + blobKey;

    public static IAmazonS3 CreateClient(BlobStorageOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.R2ServiceUrl)
            || string.IsNullOrWhiteSpace(settings.R2AccessKeyId)
            || string.IsNullOrWhiteSpace(settings.R2SecretAccessKey))
        {
            throw new InvalidOperationException(
                "BlobStorage Provider=R2 requires R2ServiceUrl, R2AccessKeyId, and R2SecretAccessKey.");
        }

        // AWSSDK.S3 v4 defaults to SigV4 (required by R2; SigV2 is unsupported).
        var credentials = new BasicAWSCredentials(settings.R2AccessKeyId, settings.R2SecretAccessKey);
        var serviceUrl = settings.R2ServiceUrl.TrimEnd('/');
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            // Local S3-compatible emulators (MinIO) run on http; force the SDK to honour it
            // so generated presigned PUT/GET URLs are reachable from the client.
            UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
        };

        return new AmazonS3Client(credentials, config);
    }

    private static Uri WithEndpointScheme(string presignedUrl, BlobStorageOptions settings)
    {
        // AWSSDK.S3 v4 can emit https for presigned URLs even when ServiceURL is http.
        var uri = new Uri(presignedUrl);
        if (string.IsNullOrEmpty(settings.R2ServiceUrl))
        {
            return uri;
        }

        var endpointScheme = settings.R2ServiceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? Uri.UriSchemeHttps
            : Uri.UriSchemeHttp;

        if (uri.Scheme.Equals(endpointScheme, StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        var builder = new UriBuilder(uri)
        {
            Scheme = endpointScheme,
            Port = uri.IsDefaultPort ? -1 : uri.Port,
        };
        return builder.Uri;
    }

    public Task<BlobUploadDescriptor> CreateUploadAsync(
        string blobKey,
        string contentType,
        long byteLength,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var ttl = Math.Max(1, _options.UploadUrlTtlMinutes);
        var expires = DateTime.UtcNow.AddMinutes(ttl);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = StorageKey(blobKey),
            Verb = HttpVerb.PUT,
            Expires = expires,
            ContentType = contentType,
        };

        var url = _s3.GetPreSignedURL(request);
        _logger.LogDebug("Created R2 upload URL for {BlobKey} expiring {ExpiresAt:o}", blobKey, expires);

        return Task.FromResult(new BlobUploadDescriptor(
            blobKey,
            WithEndpointScheme(url, _options),
            expires,
            contentType,
            byteLength,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = contentType,
            }));
    }

    public async Task<BlobObjectInfo?> GetBlobAsync(
        string blobKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);

        try
        {
            using var response = await _s3.GetObjectAsync(_bucket, StorageKey(blobKey), cancellationToken);
            await using var stream = response.ResponseStream;
            using var sha = SHA256.Create();
            await using var crypto = new CryptoStream(stream, sha, CryptoStreamMode.Read);
            // Drain the stream so the hash is computed; discard the bytes but track length
            // in case the response omits Content-Length.
            var buffer = new byte[8192];
            long bytesDrained = 0;
            int read;
            while ((read = await crypto.ReadAsync(buffer, cancellationToken)) > 0)
            {
                bytesDrained += read;
            }

            var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
            var contentType = response.Headers.ContentType ?? "application/octet-stream";
            var length = response.ContentLength > 0 ? response.ContentLength : bytesDrained;

            return new BlobObjectInfo(blobKey, contentType, length, hash);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<Uri> CreateDownloadUrlAsync(
        string blobKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);

        var publicBase = _options.R2PublicBaseUrl;
        if (!string.IsNullOrWhiteSpace(publicBase))
        {
            var builder = new UriBuilder(publicBase.TrimEnd('/'))
            {
                Path = CombinePath(publicBase, StorageKey(blobKey)),
            };
            return Task.FromResult(builder.Uri);
        }

        var ttl = Math.Max(1, _options.DownloadUrlTtlMinutes);
        var expires = DateTime.UtcNow.AddMinutes(ttl);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = StorageKey(blobKey),
            Verb = HttpVerb.GET,
            Expires = expires,
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(WithEndpointScheme(url, _options));
    }

    private static string CombinePath(string publicBase, string key)
    {
        var basePath = new Uri(publicBase.TrimEnd('/')).AbsolutePath.TrimEnd('/');
        var keyPath = key.StartsWith('/') ? key : "/" + key;
        return basePath + keyPath;
    }

    public async Task DeleteAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);
        await _s3.DeleteObjectAsync(_bucket, StorageKey(blobKey), cancellationToken);
    }

    public void Dispose() => _s3.Dispose();
}
