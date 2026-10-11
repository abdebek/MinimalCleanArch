using MinimalCleanArch.Storage;

namespace MCA.Endpoints;

public static class StorageEndpoints
{
    public static void MapStorageEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization = false)
    {
        var group = app.MapGroup("/api/storage").WithTags("Storage");

        if (requireAuthorization)
        {
            group.RequireAuthorization();
        }

        group.MapPost("/upload-url", async (
            CreateUploadUrlRequest request,
            IBlobStorage blobStorage,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(request.BlobKey))
            {
                errors["blobKey"] = ["Blob key is required."];
            }
            if (string.IsNullOrWhiteSpace(request.ContentType))
            {
                errors["contentType"] = ["Content type is required."];
            }
            if (request.ByteLength <= 0)
            {
                errors["byteLength"] = ["Byte length must be greater than zero."];
            }
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var descriptor = await blobStorage.CreateUploadAsync(
                request.BlobKey.Trim(),
                request.ContentType.Trim(),
                request.ByteLength,
                cancellationToken);

            return Results.Ok(new
            {
                descriptor.BlobKey,
                UploadUrl = descriptor.UploadUrl.ToString(),
                descriptor.ExpiresAt,
                descriptor.ContentType,
                descriptor.ByteLength,
                descriptor.RequiredHeaders
            });
        })
        .WithName("CreateBlobUploadUrl")
        .WithSummary("Create a time-limited signed URL for direct client upload to blob storage");

        group.MapGet("/download-url", async (
            string blobKey,
            IBlobStorage blobStorage,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(blobKey))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["blobKey"] = ["Blob key is required."]
                });
            }

            var url = await blobStorage.CreateDownloadUrlAsync(blobKey.Trim(), cancellationToken);
            return Results.Ok(new { blobKey = blobKey.Trim(), downloadUrl = url.ToString() });
        })
        .WithName("CreateBlobDownloadUrl")
        .WithSummary("Create a time-limited signed URL for downloading a blob");
    }

    public sealed record CreateUploadUrlRequest(string BlobKey, string ContentType, long ByteLength);
}
