using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Sample.API.Models;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Specifications;
using Microsoft.OpenApi.Models;

namespace MinimalCleanArch.Sample.API.Endpoints;

/// <summary>
/// Todo endpoints
/// </summary>
public static class TodoEndpoints
{
    public static WebApplication MapTodoEndpoints(this WebApplication app)
    {
        var todoApi = app.MapGroup("/api/todos")
            .WithTags("Todos");

        // Get all todos
        todoApi.MapGet("/", GetTodos)
            .WithName("GetTodos")
            .WithOpenApi(op => new OpenApiOperation(op)
            {
                Summary = "Gets all todos with optional filtering",
                Description = "Gets a list of todos with optional filtering and pagination"
            })
            .WithStandardResponses<object>();

        // Get todo by ID
        todoApi.MapGet("/{id:int}", GetTodoById)
            .WithName("GetTodoById")
            .WithOpenApi(op => new OpenApiOperation(op)
            {
                Summary = "Gets a todo by ID",
                Description = "Gets a todo by its ID"
            })
            .WithStandardResponses<TodoResponse>()
            .WithErrorHandling();

        // Create todo
        todoApi.MapPost("/", CreateTodo)
            .WithName("CreateTodo")
            .WithOpenApi(op => new OpenApiOperation(op)
            {
                Summary = "Creates a new todo",
                Description = "Creates a new todo"
            })
            .WithValidation<CreateTodoRequest>()
            .WithStandardResponses<TodoResponse>()
            .WithErrorHandling();

        // Update todo
        todoApi.MapPut("/{id:int}", UpdateTodo)
            .WithName("UpdateTodo")
            .WithOpenApi(op => new OpenApiOperation(op)
            {
                Summary = "Updates a todo",
                Description = "Updates a todo"
            })
            .WithValidation<UpdateTodoRequest>()
            .WithStandardResponses<TodoResponse>()
            .WithErrorHandling();

        // Delete todo
        todoApi.MapDelete("/{id:int}", DeleteTodo)
            .WithName("DeleteTodo")
            .WithOpenApi(op => new OpenApiOperation(op)
            {
                Summary = "Deletes a todo",
                Description = "Deletes a todo"
            })
            .WithStandardResponses<object>()
            .WithErrorHandling();

        return app;
    }

    // Handler implementations with UnitOfWork pattern
    private static async Task<IResult> GetTodos(
        IRepository<Todo> repository,
        string? searchTerm = null,
        bool? isCompleted = null,
        DateTime? dueBefore = null,
        DateTime? dueAfter = null,
        int? priority = null,
        int pageSize = 10,
        int pageIndex = 1)
    {
        try
        {
            // Validate parameters
            if (pageSize <= 0 || pageIndex <= 0)
            {
                return Results.BadRequest("Invalid pagination parameters");
            }

            // Ensure page size doesn't exceed maximum
            const int maxPageSize = 50;
            pageSize = Math.Min(maxPageSize, Math.Max(1, pageSize));

            var filterSpec = new TodoFilterSpecification(
                searchTerm,
                isCompleted,
                dueBefore,
                dueAfter,
                priority);

            // Get total count
            var totalCount = await repository.CountAsync(filterSpec.Criteria);

            // Get paginated results
            var paginatedSpec = new TodoPaginatedSpecification(
                pageSize,
                pageIndex,
                filterSpec);

            var todos = await repository.GetAsync(paginatedSpec);

            // Map to response
            var todoResponses = todos.Select(MapToResponse).ToList();

            // Create pagination header
            var paginationHeader = new
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = pageIndex,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Results.Ok(new
            {
                Items = todoResponses,
                Pagination = paginationHeader
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            // Log the exception here in a real application
            return Results.Problem(
                title: "Internal Server Error",
                detail: $"An error occurred while retrieving todos: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetTodoById(
        int id,
        IRepository<Todo> repository)
    {
        try
        {
            // Validate ID
            if (id <= 0)
            {
                return Results.BadRequest("Invalid todo ID");
            }

            var todo = await repository.GetByIdAsync(id);
            if (todo == null)
            {
                return Results.NotFound($"Todo with ID {id} was not found");
            }

            return Results.Ok(MapToResponse(todo));
        }
        catch (Exception)
        {
            // Log the exception here in a real application
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while retrieving the todo",
                statusCode: 500);
        }
    }

    private static async Task<IResult> CreateTodo(
        CreateTodoRequest request,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        try
        {
            // Validation is handled by the WithValidation filter
            var todo = new Todo(
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate);

            await repository.AddAsync(todo);
            await unitOfWork.SaveChangesAsync();

            return Results.Created(
                $"/api/todos/{todo.Id}",
                MapToResponse(todo));
        }
        catch (MinimalCleanArch.Domain.Exceptions.DomainException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            // Log the exception here in a real application
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating the todo",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateTodo(
        int id,
        UpdateTodoRequest request,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        try
        {
            // Validate ID
            if (id <= 0)
            {
                return Results.BadRequest("Invalid todo ID");
            }

            var todo = await repository.GetByIdAsync(id);
            if (todo == null)
            {
                return Results.NotFound($"Todo with ID {id} was not found");
            }

            // Update completion status first
            if (request.IsCompleted && !todo.IsCompleted)
            {
                todo.MarkAsCompleted();
            }
            else if (!request.IsCompleted && todo.IsCompleted)
            {
                todo.MarkAsNotCompleted();
            }

            // Update other properties
            todo.Update(
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate);

            await repository.UpdateAsync(todo);
            await unitOfWork.SaveChangesAsync();

            return Results.Ok(MapToResponse(todo));
        }
        catch (MinimalCleanArch.Domain.Exceptions.DomainException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            // Log the exception here in a real application
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating the todo",
                statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteTodo(
        int id,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        try
        {
            // Validate ID
            if (id <= 0)
            {
                return Results.BadRequest("Invalid todo ID");
            }

            var todo = await repository.GetByIdAsync(id);
            if (todo == null)
            {
                return Results.NotFound($"Todo with ID {id} was not found");
            }

            await repository.DeleteAsync(todo);
            await unitOfWork.SaveChangesAsync();

            return Results.NoContent();
        }
        catch (Exception)
        {
            // Log the exception here in a real application
            return Results.Problem( 
                title: "Internal Server Error",
                detail: "An error occurred while deleting the todo",
                statusCode: 500);
        }
    }

    private static TodoResponse MapToResponse(Todo todo)
    {
        return new TodoResponse
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            IsCompleted = todo.IsCompleted,
            Priority = todo.Priority,
            DueDate = todo.DueDate,
            CreatedAt = todo.CreatedAt,
            CreatedBy = todo.CreatedBy,
            LastModifiedAt = todo.LastModifiedAt,
            LastModifiedBy = todo.LastModifiedBy
        };
    }
}