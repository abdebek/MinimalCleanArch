using MinimalCleanArch.Storage;

namespace MCA.Endpoints;

public static class StorageEndpoints
{
    public static void MapStorageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storage").WithTags("Storage");

        group.MapPost("/upload-url", async (
            CreateUploadUrlRequest request,
            IBlobStorage blobStorage,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.BlobKey)
                || string.IsNullOrWhiteSpace(request.ContentType)
                || request.ByteLength <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["blobKey"] = ["Blob key is required."],
                    ["contentType"] = ["Content type is required."],
                    ["byteLength"] = ["Byte length must be greater than zero."]
                });
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
