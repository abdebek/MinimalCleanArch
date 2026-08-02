# MinimalCleanArch.Storage

Shared blob storage abstractions for MinimalCleanArch.

## Version
- Current stable: 0.1.20-preview (net9.0, net10.0).

## Why Use It
- keep blob upload and download capabilities behind a reusable infrastructure abstraction instead of baking storage-provider details into application handlers
- generate provider-backed upload and download URLs without coupling business workflows to Azure SDK types
- swap or add blob-storage implementations later while keeping application logic on a small, neutral contract

## When to Use It
- use it when the application stores files, media, exports, or other binary payloads outside the relational database
- keep it in infrastructure where storage clients, signed URL generation, and provider-specific headers belong
- skip it when the application has no blob/object storage requirement

## Dependency Direction
- Depends on: no other MCA package
- Typically referenced by: infrastructure projects and composition roots
- Can be used with: MCA apps or independently of the rest of MCA
- Do not reference from: domain projects; storage-provider concerns should stay outside the domain model

## Overview
- `IBlobStorage` for provider-neutral upload/download access
- `AzureBlobStorage` for Azure Blob Storage and Azurite-backed local development
- `R2BlobStorage` for Cloudflare R2 (S3-compatible API via AWSSDK.S3)
- Unified `BlobStorageOptions` with `Provider` = `Azure` (default) or `R2`
- DI helpers: `AddBlobStorage`, `AddAzureBlobStorage`, `AddR2BlobStorage`
- signed upload/download URL support plus blob metadata lookup

## Usage
```bash
dotnet add package MinimalCleanArch.Storage --version 0.1.20-preview
```

### Provider switch (recommended)

```csharp
// Reads BlobStorage:Provider — "Azure" (default) or "R2"
builder.Services.AddBlobStorage(builder.Configuration);
```

```json
{
  "BlobStorage": {
    "Provider": "Azure",
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "app-data",
    "UploadUrlTtlMinutes": 15,
    "DownloadUrlTtlMinutes": 15
  }
}
```

Cloudflare R2:

```json
{
  "BlobStorage": {
    "Provider": "R2",
    "R2ServiceUrl": "https://<accountid>.r2.cloudflarestorage.com",
    "R2AccessKeyId": "<access-key-id>",
    "R2SecretAccessKey": "<secret-access-key>",
    "R2BucketName": "app-data",
    "R2PublicBaseUrl": "https://cdn.example.com",
    "KeyPrefix": "uploads/",
    "UploadUrlTtlMinutes": 15,
    "DownloadUrlTtlMinutes": 15
  }
}
```

`KeyPrefix` (optional, e.g. `uploads/`) is prepended to every blob key for both
`Azure` and `R2` providers — useful for shared containers/buckets. It is
normalized to end with a `/` when non-empty.

### Explicit Azure / R2 registration

```csharp
builder.Services.AddAzureBlobStorage(builder.Configuration);
// or
builder.Services.AddR2BlobStorage(builder.Configuration);

builder.Services.AddAzureBlobStorage(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("BlobStorage")!;
    options.ContainerName = "app-data";
});
```

Application usage:
```csharp
public sealed class CreateUploadHandler(IBlobStorage blobStorage)
{
    public Task<BlobUploadDescriptor> Handle(CancellationToken cancellationToken) =>
        blobStorage.CreateUploadAsync(
            "uploads/report.pdf",
            "application/pdf",
            1024,
            cancellationToken);
}
```

Recommended guidance:
- keep blob keys, retention rules, and business validation in the application layer
- keep provider registration and connection settings in infrastructure or the host
- use Azurite for local development when targeting Azure Blob Storage in production
- use R2 API tokens with Object Read & Write; set `R2PublicBaseUrl` when objects are served from a custom domain or r2.dev
- add app-specific wrappers only when they genuinely add business meaning beyond generic blob operations

## Local Azurite Notes
- when browser clients upload directly to Azurite with SAS URLs, configure blob-service CORS for the local frontend origin
- allow the HTTP methods your app uses for uploads and downloads, typically `PUT`, `GET`, `HEAD`, and `OPTIONS`
- allow the Azure upload headers required by the client, including `x-ms-blob-type`
- this belongs to the Azurite or storage-account setup, not to `AzureBlobStorage` itself
