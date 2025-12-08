using MCA.Application.DTOs;
using MCA.Application.Services;

namespace MCA.Api.Endpoints;

/// <summary>
/// Todo API endpoints using minimal APIs.
/// </summary>
public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos")
            .WithTags("Todos")
            .WithOpenApi();

        group.MapGet("/", GetAllTodos)
            .WithName("GetAllTodos")
            .WithSummary("Get all todos");

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

    private static async Task<IResult> GetAllTodos(ITodoService todoService, CancellationToken cancellationToken)
    {
        var result = await todoService.GetAllAsync(cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Message);
    }

    private static async Task<IResult> GetTodoById(int id, ITodoService todoService, CancellationToken cancellationToken)
    {
        var result = await todoService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> CreateTodo(CreateTodoRequest request, ITodoService todoService, CancellationToken cancellationToken)
    {
        var result = await todoService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/todos/{result.Value.Id}", result.Value)
            : Results.BadRequest(result.Error.Message);
    }

    private static async Task<IResult> UpdateTodo(int id, UpdateTodoRequest request, ITodoService todoService, CancellationToken cancellationToken)
    {
        var result = await todoService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> CompleteTodo(int id, ITodoService todoService, CancellationToken cancellationToken)
    {
        var result = await todoService.CompleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error.Message);
    }

    private static async Task<IResult> DeleteTodo(int id, ITodoService todoService, CancellationToken cancellationToken)
    {
        var result = await todoService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error.Message);
    }
}
