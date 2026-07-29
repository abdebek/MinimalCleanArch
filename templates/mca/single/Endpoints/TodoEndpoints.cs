using MCA.Application.DTOs;
using MCA.Application.Commands;
#if (UseMessaging)
using Wolverine;
#else
using MCA.Application.Services;
#endif
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Extensions.Extensions;

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
            .WithSummary("Get todos with filtering and pagination")
            .WithErrorHandling();

        group.MapGet("/{id:int}", GetTodoById)
            .WithName("GetTodoById")
            .WithSummary("Get a todo by ID")
            .WithErrorHandling();

        group.MapPost("/", CreateTodo)
            .WithName("CreateTodo")
            .WithSummary("Create a new todo")
            .WithErrorHandling();

        group.MapPut("/{id:int}", UpdateTodo)
            .WithName("UpdateTodo")
            .WithSummary("Update an existing todo")
            .WithErrorHandling();

        group.MapPost("/{id:int}/complete", CompleteTodo)
            .WithName("CompleteTodo")
            .WithSummary("Mark a todo as completed")
            .WithErrorHandling();

        group.MapDelete("/{id:int}", DeleteTodo)
            .WithName("DeleteTodo")
            .WithSummary("Delete a todo")
            .WithErrorHandling();
    }

#if (UseMessaging)
    private static async Task<IResult> GetTodos(
        [AsParameters] TodoListRequest request,
        HttpContext httpContext,
        IMessageBus bus,
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
        if (await httpContext.ValidateAsync(query, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await bus.InvokeAsync<Result<TodoListResult>>(query, cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value));
    }

    private static async Task<IResult> GetTodoById(
        int id,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var query = new GetTodoByIdQuery(id);
#if (UseValidation)
        if (await httpContext.ValidateAsync(query, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await bus.InvokeAsync<Result<TodoResponse>>(query, cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value));
    }

    private static async Task<IResult> CreateTodo(
        CreateTodoRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new CreateTodoCommand(request.Title, request.Description, request.Priority, request.DueDate);
#if (UseValidation)
        if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await bus.InvokeAsync<Result<TodoResponse>>(command, cancellationToken);
        return result.MatchHttp(
            httpContext,
            value => Results.Created($"/api/todos/{value.Id}", value));
    }

    private static async Task<IResult> UpdateTodo(
        int id,
        UpdateTodoRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTodoCommand(id, request.Title, request.Description, request.Priority, request.DueDate);
#if (UseValidation)
        if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await bus.InvokeAsync<Result<TodoResponse>>(command, cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value));
    }

    private static async Task<IResult> CompleteTodo(
        int id,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new CompleteTodoCommand(id);
#if (UseValidation)
        if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await bus.InvokeAsync<Result>(command, cancellationToken);
        return result.MatchHttp(httpContext, () => Results.NoContent());
    }

    private static async Task<IResult> DeleteTodo(
        int id,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTodoCommand(id);
#if (UseValidation)
        if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await bus.InvokeAsync<Result>(command, cancellationToken);
        return result.MatchHttp(httpContext, () => Results.NoContent());
    }

#else
    private static async Task<IResult> GetTodos(
        [AsParameters] TodoListRequest request,
        HttpContext httpContext,
        ITodoService todoService,
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
        if (await httpContext.ValidateAsync(query, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await todoService.GetListAsync(request, cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value));
    }

    private static async Task<IResult> GetTodoById(
        int id,
        HttpContext httpContext,
        ITodoService todoService,
        CancellationToken cancellationToken)
    {
        var query = new GetTodoByIdQuery(id);
#if (UseValidation)
        if (await httpContext.ValidateAsync(query, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await todoService.GetByIdAsync(id, cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value));
    }

    private static async Task<IResult> CreateTodo(
        CreateTodoRequest request,
        HttpContext httpContext,
        ITodoService todoService,
        CancellationToken cancellationToken)
    {
        var command = new CreateTodoCommand(request.Title, request.Description, request.Priority, request.DueDate);
#if (UseValidation)
        if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await todoService.CreateAsync(request, cancellationToken);
        return result.MatchHttp(
            httpContext,
            value => Results.Created($"/api/todos/{value.Id}", value));
    }

    private static async Task<IResult> UpdateTodo(
        int id,
        UpdateTodoRequest request,
        HttpContext httpContext,
        ITodoService todoService,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTodoCommand(id, request.Title, request.Description, request.Priority, request.DueDate);
#if (UseValidation)
        if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await todoService.UpdateAsync(id, request, cancellationToken);
        return result.MatchHttp(httpContext, value => Results.Ok(value));
    }

    private static async Task<IResult> CompleteTodo(
        int id,
        HttpContext httpContext,
        ITodoService todoService,
        CancellationToken cancellationToken)
    {
        var command = new CompleteTodoCommand(id);
#if (UseValidation)
        if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await todoService.CompleteAsync(id, cancellationToken);
        return result.MatchHttp(httpContext, () => Results.NoContent());
    }

    private static async Task<IResult> DeleteTodo(
        int id,
        HttpContext httpContext,
        ITodoService todoService,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTodoCommand(id);
#if (UseValidation)
        if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }
#endif

        var result = await todoService.DeleteAsync(id, cancellationToken);
        return result.MatchHttp(httpContext, () => Results.NoContent());
    }
#endif
}
