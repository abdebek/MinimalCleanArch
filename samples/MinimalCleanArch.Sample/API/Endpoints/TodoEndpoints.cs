using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Sample.API.Models;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Specifications;

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
            .WithSummary("Gets all todos with optional filtering")
            .WithDescription("Gets a list of todos with optional filtering and pagination")
            .WithStandardResponses<object>()
            .WithErrorHandling();

        // Get todo by ID
        todoApi.MapGet("/{id:int}", GetTodoById)
            .WithName("GetTodoById")
            .WithSummary("Gets a todo by ID")
            .WithDescription("Gets a todo by its ID")
            .WithStandardResponses<TodoResponse>()
            .WithErrorHandling();

        // Create todo
        todoApi.MapPost("/", CreateTodo)
            .WithName("CreateTodo")
            .WithSummary("Creates a new todo")
            .WithDescription("Creates a new todo")
            .WithValidation<CreateTodoRequest>()
            .WithStandardResponses<TodoResponse>()
            .WithErrorHandling();

        // Update todo
        todoApi.MapPut("/{id:int}", UpdateTodo)
            .WithName("UpdateTodo")
            .WithSummary("Updates a todo")
            .WithDescription("Updates a todo")
            .WithValidation<UpdateTodoRequest>()
            .WithStandardResponses<TodoResponse>()
            .WithErrorHandling();

        // Delete todo
        todoApi.MapDelete("/{id:int}", DeleteTodo)
            .WithName("DeleteTodo")
            .WithSummary("Deletes a todo")
            .WithDescription("Deletes a todo")
            .WithStandardResponses<object>()
            .WithErrorHandling();

        return app;
    }

    private static async Task<IResult> GetTodos(
        HttpContext context,
        IRepository<Todo> repository,
        string? searchTerm = null,
        bool? isCompleted = null,
        DateTime? dueBefore = null,
        DateTime? dueAfter = null,
        int? priority = null,
        int pageSize = 10,
        int pageIndex = 1)
    {
        var paginationValidation = ValidatePagination(pageSize, pageIndex);
        if (paginationValidation.IsFailure)
        {
            return paginationValidation.Error.ToProblem(context);
        }

        const int maxPageSize = 50;
        pageSize = Math.Min(maxPageSize, Math.Max(1, pageSize));

        var filterSpec = new TodoFilterSpecification(
            searchTerm,
            isCompleted,
            dueBefore,
            dueAfter,
            priority);

        var totalCount = await repository.CountAsync(filterSpec.Criteria);

        var paginatedSpec = new TodoPaginatedSpecification(
            pageSize,
            pageIndex,
            filterSpec);

        var todos = await repository.GetAsync(paginatedSpec);
        var todoResponses = todos.Select(MapToResponse).ToList();

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

    private static async Task<IResult> GetTodoById(
        HttpContext context,
        int id,
        IRepository<Todo> repository)
    {
        var todoResult = await TryGetTodoAsync(id, repository);
        return todoResult.MatchHttp(context, todo => Results.Ok(MapToResponse(todo)));
    }

    private static async Task<IResult> CreateTodo(
        CreateTodoRequest request,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        // Request shape validation is handled by WithValidation.
        // Domain invariants can still throw DomainException and are handled by filters/middleware.
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

    private static async Task<IResult> UpdateTodo(
        HttpContext context,
        int id,
        UpdateTodoRequest request,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        var todoResult = await TryGetTodoAsync(id, repository);
        return await todoResult.Match(
            async todo =>
            {
                if (request.IsCompleted && !todo.IsCompleted)
                {
                    todo.MarkAsCompleted();
                }
                else if (!request.IsCompleted && todo.IsCompleted)
                {
                    todo.MarkAsNotCompleted();
                }

                todo.Update(
                    request.Title,
                    request.Description,
                    request.Priority,
                    request.DueDate);

                await repository.UpdateAsync(todo);
                await unitOfWork.SaveChangesAsync();

                return Results.Ok(MapToResponse(todo));
            },
            error => Task.FromResult(error.ToProblem(context)));
    }

    private static async Task<IResult> DeleteTodo(
        HttpContext context,
        int id,
        IRepository<Todo> repository,
        IUnitOfWork unitOfWork)
    {
        var todoResult = await TryGetTodoAsync(id, repository);
        return await todoResult.Match(
            async todo =>
            {
                await repository.DeleteAsync(todo);
                await unitOfWork.SaveChangesAsync();
                return Results.NoContent();
            },
            error => Task.FromResult(error.ToProblem(context)));
    }

    private static Result ValidatePagination(int pageSize, int pageIndex)
    {
        if (pageSize > 0 && pageIndex > 0)
        {
            return Result.Success();
        }

        return Error.Validation("INVALID_PAGINATION", "Invalid pagination parameters")
            .WithMetadata("PageSize", pageSize)
            .WithMetadata("PageIndex", pageIndex);
    }

    private static Result ValidateTodoId(int id)
    {
        if (id > 0)
        {
            return Result.Success();
        }

        return Error.Validation("INVALID_TODO_ID", "Invalid todo ID", "id")
            .WithMetadata("TodoId", id);
    }

    private static async Task<Result<Todo>> TryGetTodoAsync(int id, IRepository<Todo> repository)
    {
        var idValidation = ValidateTodoId(id);
        if (idValidation.IsFailure)
        {
            return Result.Failure<Todo>(idValidation.Error);
        }

        var todo = await repository.GetByIdAsync(id);
        if (todo is null)
        {
            return Result.Failure<Todo>(
                Error.NotFound("TODO_NOT_FOUND", $"Todo with ID {id} was not found")
                    .WithMetadata("TodoId", id));
        }

        return Result.Success(todo);
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
