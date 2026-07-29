using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Repositories;
#if (UseCaching)
using Microsoft.Extensions.Caching.Memory;
#endif

namespace MCA.Application.Handlers;

/// <summary>
/// Todo use-case handlers (CQRS). Invoked via Wolverine when messaging is enabled,
/// or directly from endpoints when messaging is off.
/// </summary>
public class TodoCommandHandler
{
    private readonly ITodoRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
#if (UseCaching)
    private readonly IMemoryCache _cache;
    private const string AllTodosCacheKey = "todos_all";
#endif

    public TodoCommandHandler(
        ITodoRepository repository,
        IUnitOfWork unitOfWork
#if (UseCaching)
        , IMemoryCache cache
#endif
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
#if (UseCaching)
        _cache = cache;
#endif
    }

    public async Task<Result<TodoListResult>> Handle(GetAllTodosQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<Todo> todos;
#if (UseCaching)
        if (_cache.TryGetValue(AllTodosCacheKey, out IReadOnlyList<Todo>? cached) && cached is not null)
        {
            todos = cached;
        }
        else
        {
            todos = await _repository.GetAllAsync(cancellationToken);
            _cache.Set(AllTodosCacheKey, todos, TimeSpan.FromMinutes(2));
        }
#else
        todos = await _repository.GetAllAsync(cancellationToken);
#endif
        var items = todos.Select(MapToResponse).ToList();
        return Result.Success(new TodoListResult(items));
    }

    public async Task<Result<TodoResponse>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken)
    {
        Todo? todo;
#if (UseCaching)
        if (_cache.TryGetValue(GetTodoCacheKey(query.Id), out Todo? cached) && cached is not null)
        {
            todo = cached;
        }
        else
        {
            todo = await _repository.GetByIdAsync(query.Id, cancellationToken);
            if (todo is not null)
            {
                _cache.Set(GetTodoCacheKey(query.Id), todo, TimeSpan.FromMinutes(5));
            }
        }
#else
        todo = await _repository.GetByIdAsync(query.Id, cancellationToken);
#endif

        return todo is null
            ? Result.Failure<TodoResponse>(Error.NotFound("Todo.NotFound", $"Todo with id {query.Id} not found"))
            : Result.Success(MapToResponse(todo));
    }

    public async Task<Result<TodoResponse>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = new Todo(command.Title, command.Description, command.Priority, command.DueDate);
        await _repository.AddAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif
        return Result.Success(MapToResponse(todo));
    }

    public async Task<Result<TodoResponse>> Handle(UpdateTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure<TodoResponse>(Error.NotFound("Todo.NotFound", $"Todo with id {command.Id} not found"));
        }

        todo.Update(command.Title, command.Description, command.Priority, command.DueDate);
        await _repository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif
        return Result.Success(MapToResponse(todo));
    }

    public async Task<Result> Handle(CompleteTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure(Error.NotFound("Todo.NotFound", $"Todo with id {command.Id} not found"));
        }

        todo.MarkAsCompleted();
        await _repository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif
        return Result.Success();
    }

    public async Task<Result> Handle(DeleteTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure(Error.NotFound("Todo.NotFound", $"Todo with id {command.Id} not found"));
        }

        todo.Delete();
        await _repository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif
        return Result.Success();
    }

    private static TodoResponse MapToResponse(Todo todo) =>
        new(
            todo.Id,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.Priority,
            todo.DueDate,
            todo.CreatedAt,
            todo.LastModifiedAt);

#if (UseCaching)
    private static string GetTodoCacheKey(int id) => $"todo_{id}";

    private void InvalidateCache(int id)
    {
        _cache.Remove(AllTodosCacheKey);
        _cache.Remove(GetTodoCacheKey(id));
    }
#endif
}
