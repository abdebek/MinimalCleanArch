# MinimalCleanArch Sample Application

A comprehensive sample application demonstrating MinimalCleanArch usage with modern patterns and best practices.

## Overview

This sample demonstrates:

- **Clean Architecture** - Proper separation of concerns across layers
- **Domain Entities** - Rich domain models with business rules and validation
- **Entity Framework Core** - Data access with soft delete and auditing
- **Unit of Work Pattern** - Proper transaction management and data persistence
- **Minimal API** - Modern API endpoints with validation and error handling
- **Column-level Encryption** - Sensitive data protection with AES encryption
- **Specification Pattern** - Flexible and reusable query logic
- **Validation** - Request validation using FluentValidation
- **Error Handling** - Global error handling middleware

## Features Showcased

### 🏗️ Architecture Patterns
- **Repository Pattern** - Clean data access abstraction
- **Unit of Work** - Transaction boundary management
- **Specification Pattern** - Encapsulated query logic
- **Domain-Driven Design** - Rich domain models

### 🔒 Security Features
- **Data Encryption** - Automatic encryption of sensitive fields
- **Input Validation** - Comprehensive request validation
- **Error Handling** - Secure error responses without information leakage

### 🚀 Modern API Features
- **Minimal APIs** - Lightweight, high-performance endpoints
- **OpenAPI/Swagger** - Auto-generated API documentation
- **Validation Filters** - Automatic request validation
- **Error Middleware** - Consistent error responses

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- SQLite (included)
- MinimalCleanArch packages v0.1.6 (if using a local feed, add `nuget.config` pointing to it before restore)

### Running the Application

1. **Clone and navigate to the sample:**
   ```bash
   cd samples/MinimalCleanArch.Sample
   ```

2. **Configure encryption (optional):**
   Update `appsettings.json`:
   ```json
   {
     "Encryption": {
       "Key": "your-super-strong-encryption-key-at-least-32-characters-long"
     }
   }
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Browse the API:**
   - Swagger UI: http://localhost:5096
   - Health Check: http://localhost:5096/health

## API Endpoints

### Todo Management
- `GET /api/todos` - Get todos with filtering and pagination
- `GET /api/todos/{id}` - Get a specific todo
- `POST /api/todos` - Create a new todo
- `PUT /api/todos/{id}` - Update a todo
- `DELETE /api/todos/{id}` - Delete a todo (soft delete)

### Query Parameters
- `searchTerm` - Search in title and description
- `isCompleted` - Filter by completion status
- `priority` - Filter by priority level (0-5)
- `dueBefore` / `dueAfter` - Filter by due date range
- `pageIndex` / `pageSize` - Pagination controls

## Example Usage

### Creating a Todo
```bash
curl -X POST "http://localhost:5096/api/todos" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Learn MinimalCleanArch",
    "description": "Study the sample application",
    "priority": 3,
    "dueDate": "2024-12-31"
  }'
```

### Querying Todos
```bash
# Get high priority incomplete todos
curl "http://localhost:5096/api/todos?isCompleted=false&priority=5"

# Search todos with pagination
curl "http://localhost:5096/api/todos?searchTerm=learn&pageSize=10&pageIndex=1"
```

## Key Implementation Details

### Domain Model
```csharp
public class Todo : BaseSoftDeleteEntity
{
    [Encrypted] // Automatically encrypted in database
    public string Description { get; private set; }
    
    // Rich domain methods
    public void MarkAsCompleted() { /* business logic */ }
}
```

### Repository Usage with Unit of Work
```csharp
// Create with transaction
return await unitOfWork.ExecuteInTransactionAsync(async () =>
{
    await repository.AddAsync(todo);
    await unitOfWork.SaveChangesAsync();
    return Results.Created($"/api/todos/{todo.Id}", todo);
});
```

### Specification Pattern
```csharp
var filterSpec = new TodoFilterSpecification(
    searchTerm: "important",
    isCompleted: false,
    priority: 3);

var todos = await repository.GetAsync(filterSpec);
```

## Project Structure

```
MinimalCleanArch.Sample/
├── API/
│   ├── Endpoints/          # Minimal API endpoint definitions
│   ├── Models/             # Request/Response DTOs
│   └── Validators/         # FluentValidation validators
├── Domain/
│   └── Entities/           # Domain entities with business logic
├── Infrastructure/
│   ├── Data/               # DbContext and entity configurations
│   └── Specifications/     # Query specifications
├── Program.cs              # Application startup and configuration
└── appsettings.json        # Configuration
```

## Configuration

### Database
The sample uses SQLite by default for simplicity. To use SQL Server:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MinimalCleanArchSample;Trusted_Connection=True;"
  }
}
```

### Encryption
Strong encryption keys are recommended for production:

```csharp
// Generate a strong key
var strongKey = EncryptionOptions.GenerateStrongKey(64);
```

## Learning Path

1. **Start with the API endpoints** (`API/Endpoints/TodoEndpoints.cs`)
2. **Examine the domain model** (`Domain/Entities/Todo.cs`)
3. **Review the specifications** (`Infrastructure/Specifications/`)
4. **Study the service registration** (`Program.cs`)
5. **Explore the validation** (`API/Validators/`)

## Next Steps (Optional, Needs to be reviewed)

- Add authentication and authorization
- Implement caching with Redis
- Add integration tests
- Deploy to cloud platform
- Implement CQRS pattern
- Add domain events
