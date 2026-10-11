using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Application.Specifications;
using MCA.Domain;
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
    private readonly ITodoRepository _todoRepository;
    private readonly IUnitOfWork _unitOfWork;
#if (UseCaching)
    private readonly ICacheService _cache;
    private static readonly CacheEntryOptions ItemCacheOptions = CacheEntryOptions.Absolute(TimeSpan.FromMinutes(5));
#endif
#if (UseRealtime)
    private readonly IRealtimePublisher _realtime;
#endif

    public TodoCommandHandler(
        ITodoRepository todoRepository,
        IUnitOfWork unitOfWork
#if (UseCaching)
        , ICacheService cache
#endif
#if (UseRealtime)
        , IRealtimePublisher realtime
#endif
        )
    {
        _todoRepository = todoRepository;
        _unitOfWork = unitOfWork;
#if (UseCaching)
        _cache = cache;
#endif
#if (UseRealtime)
        _realtime = realtime;
#endif
    }

    public async Task<Result<TodoListResult>> Handle(GetTodosQuery query, CancellationToken cancellationToken)
    {
        const int maxPageSize = 100;

        if (query.PageSize <= 0)
        {
            return Result.Failure<TodoListResult>(DomainErrors.Pagination.InvalidPageSize(query.PageSize));
        }

        if (query.PageIndex <= 0)
        {
            return Result.Failure<TodoListResult>(DomainErrors.Pagination.InvalidPageIndex(query.PageIndex));
        }

        if (query.PageSize > maxPageSize)
        {
            return Result.Failure<TodoListResult>(DomainErrors.Pagination.PageSizeTooLarge(query.PageSize, maxPageSize));
        }

        var filterSpec = new TodoFilterSpecification(
            query.SearchTerm,
            query.IsCompleted,
            query.DueBefore,
            query.DueAfter,
            query.Priority);

        var totalCount = await _todoRepository.CountAsync(filterSpec, cancellationToken);

        var pagedSpec = new TodoPaginatedSpecification(
            query.PageSize,
            query.PageIndex,
            filterSpec);

        var todos = await _todoRepository.GetAsync(pagedSpec, cancellationToken);
        var items = todos.Select(MapToResponse).ToList();

        return Result.Success(new TodoListResult(
            items,
            totalCount,
            query.PageIndex,
            query.PageSize));
    }

    public async Task<Result<TodoResponse>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken)
    {
        TodoResponse? response;
#if (UseCaching)
        // Cache DTOs, not Todo entities: Redis JSON cannot round-trip private setters.
        response = await _cache.GetOrCreateAsync(
            GetTodoCacheKey(query.Id),
            async ct =>
            {
                var todo = await _todoRepository.GetFirstAsync(new TodoByIdSpecification(query.Id), ct);
                return todo is null ? null : MapToResponse(todo);
            },
            ItemCacheOptions,
            cancellationToken);
#else
        var todo = await _todoRepository.GetFirstAsync(new TodoByIdSpecification(query.Id), cancellationToken);
        response = todo is null ? null : MapToResponse(todo);
#endif
        return response is null
            ? Result.Failure<TodoResponse>(DomainErrors.General.NotFound(nameof(Todo), query.Id))
            : Result.Success(response);
    }

    public async Task<Result<TodoResponse>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = new Todo(command.Title, command.Description, command.Priority, command.DueDate);
        await _todoRepository.AddAsync(todo, cancellationToken);
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
        var todo = await _todoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure<TodoResponse>(DomainErrors.General.NotFound(nameof(Todo), command.Id));
        }

        todo.Update(command.Title, command.Description, command.Priority, command.DueDate);
        await _todoRepository.UpdateAsync(todo, cancellationToken);
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
        var todo = await _todoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure(DomainErrors.General.NotFound(nameof(Todo), command.Id));
        }

        todo.MarkAsCompleted();
        await _todoRepository.UpdateAsync(todo, cancellationToken);
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
        var todo = await _todoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure(DomainErrors.General.NotFound(nameof(Todo), command.Id));
        }

        todo.Delete();
        await _todoRepository.UpdateAsync(todo, cancellationToken);
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
        var todo = await _todoRepository.GetByIdIncludingDeletedAsync(command.Id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure<TodoResponse>(DomainErrors.General.NotFound(nameof(Todo), command.Id));
        }

        todo.Restore();
        await _todoRepository.UpdateAsync(todo, cancellationToken);
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
        new(todo.Id, todo.Title, todo.Description, todo.IsCompleted, todo.Priority, todo.DueDate);

#if (UseCaching)
    private static string GetTodoCacheKey(int id) => $"todo_{id}";

    private Task InvalidateCacheAsync(int id, CancellationToken cancellationToken) =>
        _cache.RemoveAsync(GetTodoCacheKey(id), cancellationToken);
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
