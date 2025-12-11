using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Application.Services;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Handlers;

/// <summary>
/// Wolverine handlers for Todo commands/queries (CQRS style).
/// </summary>
public class TodoCommandHandlers
{
    private readonly ITodoService _todoService;

    public TodoCommandHandlers(ITodoService todoService)
    {
        _todoService = todoService;
    }

    public async Task<Result<TodoListResult>> Handle(GetAllTodosQuery query, CancellationToken cancellationToken)
    {
        var result = await _todoService.GetAllAsync(cancellationToken);
        return result.IsSuccess
            ? Result.Success(new TodoListResult(result.Value))
            : Result.Failure<TodoListResult>(result.Error);
    }

    public Task<Result<TodoResponse>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken)
    {
        return _todoService.GetByIdAsync(query.Id, cancellationToken);
    }

    public Task<Result<TodoResponse>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        var request = new CreateTodoRequest(command.Title, command.Description, command.Priority, command.DueDate);
        return _todoService.CreateAsync(request, cancellationToken);
    }

    public Task<Result<TodoResponse>> Handle(UpdateTodoCommand command, CancellationToken cancellationToken)
    {
        var request = new UpdateTodoRequest(command.Title, command.Description, command.Priority, command.DueDate);
        return _todoService.UpdateAsync(command.Id, request, cancellationToken);
    }

    public Task<Result> Handle(CompleteTodoCommand command, CancellationToken cancellationToken)
    {
        return _todoService.CompleteAsync(command.Id, cancellationToken);
    }

    public Task<Result> Handle(DeleteTodoCommand command, CancellationToken cancellationToken)
    {
        return _todoService.DeleteAsync(command.Id, cancellationToken);
    }
}
