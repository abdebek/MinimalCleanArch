using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Repositories;
#if (UseCaching)
using MinimalCleanArch.Extensions.Caching;
#endif
#if (UseRealtime)
using MinimalCleanArch.Realtime;
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
    private readonly ICacheService _cache;
    private static readonly CacheEntryOptions ListCacheOptions = CacheEntryOptions.Absolute(TimeSpan.FromMinutes(2));
    private static readonly CacheEntryOptions ItemCacheOptions = CacheEntryOptions.Absolute(TimeSpan.FromMinutes(5));
#endif
#if (UseRealtime)
    private readonly IRealtimePublisher _realtime;
#endif

    public TodoCommandHandler(
        ITodoRepository repository,
        IUnitOfWork unitOfWork
#if (UseCaching)
        , ICacheService cache
#endif
#if (UseRealtime)
        , IRealtimePublisher realtime
#endif
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
#if (UseCaching)
        _cache = cache;
#endif
#if (UseRealtime)
        _realtime = realtime;
#endif
    }

    public async Task<Result<TodoListResult>> Handle(GetAllTodosQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<TodoResponse> items;
#if (UseCaching)
        // Cache DTOs, not Todo entities: Redis JSON cannot round-trip private setters.
        items = await _cache.GetOrCreateAsync(
            AllTodosCacheKey,
            async ct =>
            {
                var todos = await _repository.GetAllAsync(ct);
                return todos.Select(MapToResponse).ToList();
            },
            ListCacheOptions,
            cancellationToken) ?? [];
#else
        var todos = await _repository.GetAllAsync(cancellationToken);
        items = todos.Select(MapToResponse).ToList();
#endif
        return Result.Success(new TodoListResult(items));
    }

    public async Task<Result<TodoResponse>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken)
    {
        TodoResponse? response;
#if (UseCaching)
        response = await _cache.GetOrCreateAsync(
            GetTodoCacheKey(query.Id),
            async ct =>
            {
                var todo = await _repository.GetByIdAsync(query.Id, ct);
                return todo is null ? null : MapToResponse(todo);
            },
            ItemCacheOptions,
            cancellationToken);
#else
        var todo = await _repository.GetByIdAsync(query.Id, cancellationToken);
        response = todo is null ? null : MapToResponse(todo);
#endif

        return response is null
            ? Result.Failure<TodoResponse>(Error.NotFound("Todo.NotFound", $"Todo with id {query.Id} not found"))
            : Result.Success(response);
    }

    public async Task<Result<TodoResponse>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = new Todo(command.Title, command.Description, command.Priority, command.DueDate);
        await _repository.AddAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        await InvalidateCacheAsync(todo.Id, cancellationToken);
#endif
#if (UseRealtime)
        await PublishTodoChangedAsync(todo, "created", cancellationToken);
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
        await InvalidateCacheAsync(todo.Id, cancellationToken);
#endif
#if (UseRealtime)
        await PublishTodoChangedAsync(todo, "updated", cancellationToken);
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
        await InvalidateCacheAsync(todo.Id, cancellationToken);
#endif
#if (UseRealtime)
        await PublishTodoChangedAsync(todo, "completed", cancellationToken);
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
        await InvalidateCacheAsync(todo.Id, cancellationToken);
#endif
#if (UseRealtime)
        await PublishTodoChangedAsync(todo, "deleted", cancellationToken);
#endif
        return Result.Success();
    }

    public async Task<Result<TodoResponse>> Handle(RestoreTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = await _repository.GetByIdIncludingDeletedAsync(command.Id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure<TodoResponse>(Error.NotFound("Todo.NotFound", $"Todo with id {command.Id} not found"));
        }

        todo.Restore();
        await _repository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        await InvalidateCacheAsync(todo.Id, cancellationToken);
#endif
#if (UseRealtime)
        await PublishTodoChangedAsync(todo, "restored", cancellationToken);
#endif
        return Result.Success(MapToResponse(todo));
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
    private const string AllTodosCacheKey = "todos_all";

    private static string GetTodoCacheKey(int id) => $"todo_{id}";

    private async Task InvalidateCacheAsync(int id, CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(AllTodosCacheKey, cancellationToken);
        await _cache.RemoveAsync(GetTodoCacheKey(id), cancellationToken);
    }
#endif

#if (UseRealtime)
    private Task PublishTodoChangedAsync(Todo todo, string action, CancellationToken cancellationToken) =>
        _realtime.PublishAsync(
            new RealtimeMessage
            {
                Channel = "todos",
                Payload = new { id = todo.Id, title = todo.Title, action },
#if (UseMultiTenant)
                TenantId = todo.TenantId,
#endif
            },
            cancellationToken);
#endif
}
