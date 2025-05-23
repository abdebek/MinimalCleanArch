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
        todoApi.MapGet("/{id}", GetTodoById)
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
        todoApi.MapPut("/{id}", UpdateTodo)
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
        todoApi.MapDelete("/{id}", DeleteTodo)
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
        [AsParameters] TodoQueryParameters parameters)
    {
        var filterSpec = new TodoFilterSpecification(
            parameters.SearchTerm,
            parameters.IsCompleted,
            parameters.DueBefore,
            parameters.DueAfter,
            parameters.Priority);

        // Get total count
        var totalCount = await repository.CountAsync(filterSpec.Criteria);

        // Get paginated results
        var paginatedSpec = new TodoPaginatedSpecification(
            parameters.PageSize,
            parameters.PageIndex,
            filterSpec);

        var todos = await repository.GetAsync(paginatedSpec);

        // Map to response
        var todoResponses = todos.Select(MapToResponse).ToList();

        // Create pagination header
        var paginationHeader = new
        {
            TotalCount = totalCount,
            PageSize = parameters.PageSize,
            CurrentPage = parameters.PageIndex,
            TotalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize)
        };

        return Results.Ok(new
        {
            Items = todoResponses,
            Pagination = paginationHeader
        });
    }

    private static async Task<IResult> GetTodoById(
        int id,
        IRepository<Todo> repository)
    {
        var todo = await repository.GetByIdAsync(id);
        if (todo == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(MapToResponse(todo));
    }

    private static async Task<IResult> CreateTodo(
        CreateTodoRequest request,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        try
        {
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
        catch (Exception)
        {
            // The error handling middleware will catch and handle this
            throw;
        }
    }

    private static async Task<IResult> UpdateTodo(
        int id,
        UpdateTodoRequest request,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var todo = await repository.GetByIdAsync(id);
            if (todo == null)
            {
                return Results.NotFound();
            }

            if (todo.IsCompleted && !request.IsCompleted)
            {
                return Results.Conflict("Cannot mark a completed todo as not completed.");
            }

            // Update completion status
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
        });
    }

    private static async Task<IResult> DeleteTodo(
        int id,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var todo = await repository.GetByIdAsync(id);
            if (todo == null)
            {
                return Results.NotFound();
            }

            await repository.DeleteAsync(todo);
            await unitOfWork.SaveChangesAsync();

            return Results.NoContent();
        });
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

/// <summary>
/// Parameters for querying todos
/// </summary>
public class TodoQueryParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    /// <summary>
    /// Gets or sets the page index
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(MaxPageSize, Math.Max(1, value));
    }

    /// <summary>
    /// Gets or sets the search term
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the completion status filter
    /// </summary>
    public bool? IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets the due before filter
    /// </summary>
    public DateTime? DueBefore { get; set; }

    /// <summary>
    /// Gets or sets the due after filter
    /// </summary>
    public DateTime? DueAfter { get; set; }

    /// <summary>
    /// Gets or sets the priority filter
    /// </summary>
    public int? Priority { get; set; }
}