using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Application.Services;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Handlers;

/// <summary>
/// Wolverine handlers for Todo commands/queries (CQRS style).
/// </summary>
public class TodoCommandHandler
{
    private readonly ITodoService _todoService;

    public TodoCommandHandler(ITodoService todoService)
    {
        _todoService = todoService;
    }

    public async Task<Result<TodoListResult>> Handle(GetTodosQuery query, CancellationToken cancellationToken)
    {
        var request = new TodoListRequest(
            query.SearchTerm,
            query.IsCompleted,
            query.Priority,
            query.DueBefore,
            query.DueAfter,
            query.PageIndex,
            query.PageSize);
        var result = await _todoService.GetListAsync(request, cancellationToken);
        return result.IsSuccess
            ? Result.Success(new TodoListResult(
                result.Value.Items,
                result.Value.TotalCount,
                result.Value.PageIndex,
                result.Value.PageSize))
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
