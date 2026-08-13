# Context: Blob storage

**Boundary:** NuGet `MinimalCleanArch.Storage` plus template `StorageEndpoints` when `--storage` or `--all`.  
**Kind:** Infrastructure. Independent of other MCA packages.  
**Database:** none. Object store is Azure Blob (Azurite locally) or Cloudflare R2.  
**UI:** none. Clients upload to signed URLs.

## What it owns

| Type | Path |
|---|---|
| `IBlobStorage` | `src/MinimalCleanArch.Storage/IBlobStorage.cs` |
| `BlobUploadDescriptor`, `BlobObjectInfo` | same |
| `AzureBlobStorage`, `R2BlobStorage` | package root |
| `BlobKeyValidator` | `BlobKeyValidator.cs` |
| `AddBlobStorage` | `ServiceCollectionExtensions.cs` |
| Template endpoints | `templates/mca/single/Endpoints/StorageEndpoints.cs` |

Operations: `CreateUploadAsync`, `GetBlobAsync`, `CreateDownloadUrlAsync`, `DeleteAsync`.

## Aggregates

None. There is no `Blob` entity and no FK from `Todo`.

## Commands / queries / hosts

Template HTTP (optional `RequireAuthorization` if auth is on):

| Route | Behavior |
|---|---|
| `POST /api/storage/upload-url` | Validates key, content type, byte length; returns signed upload URL + required headers |
| `GET /api/storage/download-url?blobKey=` | Returns signed download URL |

The sample app does **not** reference Storage.

## Integration

Host calls `AddBlobStorage(configuration)` (`BlobStorage` section). With `--docker`, compose adds Azurite. Domain must stay free of this package (`ArchitectureTests`).

## Honesty

Endpoints talk to `IBlobStorage` directly. No application handler, no domain event, no outbox for deletes.
