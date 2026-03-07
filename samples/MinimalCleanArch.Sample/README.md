# MinimalCleanArch Sample Application

Reference app showing how the library pieces work together in a real API.

## What You Can Try
- Todo CRUD with filtering and pagination
- FluentValidation request validation
- Standardized error handling and ProblemDetails responses
- Soft delete and auditing
- Column-level encryption for sensitive user fields
- Health checks (`/health`, `/health/ready`, `/health/live`, `/health/detailed`)
- Scalar/OpenAPI exploration in Development

## Quick Run

```bash
cd samples/MinimalCleanArch.Sample
dotnet run
```

Open:
- `https://localhost:5095/scalar/v1`
- `http://localhost:5096/scalar/v1`

## Quick Feature Walkthrough

### 1) Create a Todo

```bash
curl -X POST "http://localhost:5096/api/todos" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Learn MinimalCleanArch",
    "description": "Run through the sample app features",
    "priority": 3,
    "dueDate": "2030-12-31"
  }'
```

### 2) Query Todos with Filters

```bash
curl "http://localhost:5096/api/todos?searchTerm=learn&pageSize=10&pageIndex=1"
curl "http://localhost:5096/api/todos?isCompleted=false&priority=3"
```

### 3) Trigger Validation Errors

```bash
curl -X POST "http://localhost:5096/api/todos" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "",
    "description": "invalid",
    "priority": 99
  }'
```

Expected: `400` with validation details.

### 4) Trigger Not Found/Error Mapping

```bash
curl "http://localhost:5096/api/todos/999999"
```

Expected: standardized not-found/problem response.

### 5) Check Health Endpoints

```bash
curl "http://localhost:5096/health"
curl "http://localhost:5096/health/ready"
curl "http://localhost:5096/health/live"
curl "http://localhost:5096/health/detailed"
```

## User Endpoints in the Sample

Public:
- `POST /api/users/register`
- `POST /api/users/login`

Authenticated:
- `GET /api/users/profile`
- `PUT /api/users/profile`
- `GET /api/users/todos`

Admin policy (`Admin` role):
- `GET /api/admin/users`
- `DELETE /api/admin/users/{userId}`
- `POST /api/admin/users/{userId}/roles/{roleName}`
- `DELETE /api/admin/users/{userId}/roles/{roleName}`

## Project Structure

```text
MinimalCleanArch.Sample/
|- API/
|  |- Endpoints/
|  |- Models/
|  |- Validators/
|- Domain/
|  |- Entities/
|- Infrastructure/
|  |- Data/
|  |- Specifications/
|  |- Seeders/
|- Program.cs
|- appsettings.json
```

## Useful Files to Read

1. `Program.cs` for service registration and middleware order.
1. `API/Endpoints/TodoEndpoints.cs` for result-first endpoint patterns.
1. `API/Validators/TodoValidators.cs` for request rules.
1. `Infrastructure/Specifications/` for filtering logic.
1. `Infrastructure/Seeders/` for startup data seeding.

## What the sample is meant to show
- specifications composed in application-facing query paths
- validators registered once and reused by endpoints
- result-to-HTTP mapping without controller-specific plumbing
- encrypted properties and audit support without leaking infrastructure into the domain model
- a concrete reference for how the packages fit together in one app

## Notes

- SQLite is used by default (`Data Source=minimalcleanarch.db`).
- If `Encryption:Key` is not configured, a temporary key is generated in Development.
- Scalar is mapped in Development and is the default launch URL in `Properties/launchSettings.json`.
