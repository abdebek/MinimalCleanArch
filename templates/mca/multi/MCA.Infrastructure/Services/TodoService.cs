using MCA.Application.DTOs;
using MCA.Application.Services;
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Repositories;
#if (UseCaching)
using Microsoft.Extensions.Caching.Memory;
#endif

namespace MCA.Infrastructure.Services;

/// <summary>
/// Service implementation for Todo operations.
/// </summary>
public class TodoService : ITodoService
{
    private readonly ITodoRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
#if (UseCaching)
    private readonly IMemoryCache _cache;
    private const string AllTodosCacheKey = "todos_all";
#endif

    public TodoService(ITodoRepository repository, IUnitOfWork unitOfWork
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

    public async Task<Result<TodoResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Todo? todo;
#if (UseCaching)
        if (_cache.TryGetValue(GetTodoCacheKey(id), out Todo? cached) && cached is not null)
        {
            todo = cached;
        }
        else
        {
            todo = await _repository.GetByIdAsync(id, cancellationToken);
            if (todo is not null)
            {
                _cache.Set(GetTodoCacheKey(id), todo, TimeSpan.FromMinutes(5));
            }
        }
#else
        todo = await _repository.GetByIdAsync(id, cancellationToken);
#endif

        if (todo is null)
        {
            return Result.Failure<TodoResponse>(Error.NotFound("Todo.NotFound", $"Todo with id {id} not found"));
        }

        return Result.Success(MapToResponse(todo));
    }

    public async Task<Result<IReadOnlyList<TodoResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
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
        var response = todos.Select(MapToResponse).ToList();
        return Result.Success<IReadOnlyList<TodoResponse>>(response);
    }

    public async Task<Result<TodoResponse>> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var todo = new Todo(request.Title, request.Description, request.Priority, request.DueDate);

        await _repository.AddAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif

        return Result.Success(MapToResponse(todo));
    }

    public async Task<Result<TodoResponse>> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var todo = await _repository.GetByIdAsync(id, cancellationToken);

        if (todo is null)
        {
            return Result.Failure<TodoResponse>(Error.NotFound("Todo.NotFound", $"Todo with id {id} not found"));
        }

        todo.Update(request.Title, request.Description, request.Priority, request.DueDate);

        await _repository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif

        return Result.Success(MapToResponse(todo));
    }

    public async Task<Result> CompleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var todo = await _repository.GetByIdAsync(id, cancellationToken);

        if (todo is null)
        {
            return Result.Failure(Error.NotFound("Todo.NotFound", $"Todo with id {id} not found"));
        }

        todo.MarkAsCompleted();

        await _repository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var todo = await _repository.GetByIdAsync(id, cancellationToken);

        if (todo is null)
        {
            return Result.Failure(Error.NotFound("Todo.NotFound", $"Todo with id {id} not found"));
        }

        todo.Delete();

        await _repository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif

        return Result.Success();
    }

    private static TodoResponse MapToResponse(Todo todo)
    {
        return new TodoResponse(
            todo.Id,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.Priority,
            todo.DueDate,
            todo.CreatedAt,
            todo.LastModifiedAt);
    }

#if (UseCaching)
    private static string GetTodoCacheKey(int id) => $"todo_{id}";

    private void InvalidateCache(int id)
    {
        _cache.Remove(AllTodosCacheKey);
        _cache.Remove(GetTodoCacheKey(id));
    }
#endif
}
