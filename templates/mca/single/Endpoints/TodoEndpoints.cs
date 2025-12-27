using MCA.Application.DTOs;
using MCA.Application.Commands;
#if (UseMessaging)
using Wolverine;
#else
using MCA.Application.Services;
#endif
using MinimalCleanArch.Domain.Common;
#if (UseValidation)
using FluentValidation;
using FluentValidation.Results;
using System.Linq;
#endif

namespace MCA.Endpoints;

/// <summary>
/// Todo API endpoints using minimal APIs.
/// </summary>
public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos")
            .WithTags("Todos");

        group.MapGet("/", GetTodos)
            .WithName("GetTodos")
            .WithSummary("Get todos with filtering and pagination");

        group.MapGet("/{id:int}", GetTodoById)
            .WithName("GetTodoById")
            .WithSummary("Get a todo by ID");

        group.MapPost("/", CreateTodo)
            .WithName("CreateTodo")
            .WithSummary("Create a new todo");

        group.MapPut("/{id:int}", UpdateTodo)
            .WithName("UpdateTodo")
            .WithSummary("Update an existing todo");

        group.MapPost("/{id:int}/complete", CompleteTodo)
            .WithName("CompleteTodo")
            .WithSummary("Mark a todo as completed");

        group.MapDelete("/{id:int}", DeleteTodo)
            .WithName("DeleteTodo")
            .WithSummary("Delete a todo");
    }

#if (UseMessaging)
    private static async Task<IResult> GetTodos(
        [AsParameters] TodoListRequest request,
        IMessageBus bus,
#if (UseValidation)
        IValidator<GetTodosQuery> validator,
#endif
        CancellationToken cancellationToken)
    {
        var query = new GetTodosQuery(
            request.SearchTerm,
            request.IsCompleted,
            request.Priority,
            request.DueBefore,
            request.DueAfter,
            request.PageIndex,
            request.PageSize);

#if (UseValidation)
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await bus.InvokeAsync<Result<TodoListResult>>(query, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error.Message);
    }

    private static async Task<IResult> GetTodoById(
        int id,
        IMessageBus bus,
#if (UseValidation)
        IValidator<GetTodoByIdQuery> validator,
#endif
        CancellationToken cancellationToken)
    {
        var query = new GetTodoByIdQuery(id);
#if (UseValidation)
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await bus.InvokeAsync<Result<TodoResponse>>(query, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> CreateTodo(
        CreateTodoRequest request,
        IMessageBus bus,
#if (UseValidation)
        IValidator<CreateTodoCommand> validator,
#endif
        CancellationToken cancellationToken)
    {
        var command = new CreateTodoCommand(request.Title, request.Description, request.Priority, request.DueDate);
#if (UseValidation)
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await bus.InvokeAsync<Result<TodoResponse>>(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/todos/{result.Value.Id}", result.Value)
            : Results.BadRequest(result.Error.Message);
    }

    private static async Task<IResult> UpdateTodo(
        int id,
        UpdateTodoRequest request,
        IMessageBus bus,
#if (UseValidation)
        IValidator<UpdateTodoCommand> validator,
#endif
        CancellationToken cancellationToken)
    {
        var command = new UpdateTodoCommand(id, request.Title, request.Description, request.Priority, request.DueDate);
#if (UseValidation)
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await bus.InvokeAsync<Result<TodoResponse>>(command, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> CompleteTodo(
        int id,
        IMessageBus bus,
#if (UseValidation)
        IValidator<CompleteTodoCommand> validator,
#endif
        CancellationToken cancellationToken)
    {
        var command = new CompleteTodoCommand(id);
#if (UseValidation)
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await bus.InvokeAsync<Result>(command, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> DeleteTodo(
        int id,
        IMessageBus bus,
#if (UseValidation)
        IValidator<DeleteTodoCommand> validator,
#endif
        CancellationToken cancellationToken)
    {
        var command = new DeleteTodoCommand(id);
#if (UseValidation)
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await bus.InvokeAsync<Result>(command, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error.Message);
    }

#else
    private static async Task<IResult> GetTodos(
        [AsParameters] TodoListRequest request,
        ITodoService todoService,
#if (UseValidation)
        IValidator<GetTodosQuery> validator,
#endif
        CancellationToken cancellationToken)
    {
        var query = new GetTodosQuery(
            request.SearchTerm,
            request.IsCompleted,
            request.Priority,
            request.DueBefore,
            request.DueAfter,
            request.PageIndex,
            request.PageSize);

#if (UseValidation)
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await todoService.GetListAsync(request, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error.Message);
    }

    private static async Task<IResult> GetTodoById(
        int id,
        ITodoService todoService,
#if (UseValidation)
        IValidator<GetTodoByIdQuery> validator,
#endif
        CancellationToken cancellationToken)
    {
        var query = new GetTodoByIdQuery(id);
#if (UseValidation)
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await todoService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> CreateTodo(
        CreateTodoRequest request,
        ITodoService todoService,
#if (UseValidation)
        IValidator<CreateTodoCommand> validator,
#endif
        CancellationToken cancellationToken)
    {
        var command = new CreateTodoCommand(request.Title, request.Description, request.Priority, request.DueDate);
#if (UseValidation)
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await todoService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/todos/{result.Value.Id}", result.Value)
            : Results.BadRequest(result.Error.Message);
    }

    private static async Task<IResult> UpdateTodo(
        int id,
        UpdateTodoRequest request,
        ITodoService todoService,
#if (UseValidation)
        IValidator<UpdateTodoCommand> validator,
#endif
        CancellationToken cancellationToken)
    {
        var command = new UpdateTodoCommand(id, request.Title, request.Description, request.Priority, request.DueDate);
#if (UseValidation)
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await todoService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> CompleteTodo(
        int id,
        ITodoService todoService,
#if (UseValidation)
        IValidator<CompleteTodoCommand> validator,
#endif
        CancellationToken cancellationToken)
    {
        var command = new CompleteTodoCommand(id);
#if (UseValidation)
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await todoService.CompleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> DeleteTodo(
        int id,
        ITodoService todoService,
#if (UseValidation)
        IValidator<DeleteTodoCommand> validator,
#endif
        CancellationToken cancellationToken)
    {
        var command = new DeleteTodoCommand(id);
#if (UseValidation)
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(ToDictionary(validation));
        }
#endif

        var result = await todoService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error.Message);
    }
#endif

#if (UseValidation)
    private static IDictionary<string, string[]> ToDictionary(ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
    }
#endif
}
