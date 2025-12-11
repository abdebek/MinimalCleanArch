using MCA.Application.DTOs;
using MCA.Application.Services;
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MCA.Domain;
using MCA.Infrastructure.Specifications;
using MinimalCleanArch.DataAccess.Repositories;
using MinimalCleanArch.Domain.Common;
#if (UseCaching)
using Microsoft.Extensions.Caching.Memory;
#endif

namespace MCA.Infrastructure.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private readonly IUnitOfWork _unitOfWork;
#if (UseCaching)
    private readonly IMemoryCache _cache;
#endif

    public TodoService(ITodoRepository todoRepository, IUnitOfWork unitOfWork
#if (UseCaching)
        , IMemoryCache cache
#endif
    )
    {
        _todoRepository = todoRepository;
        _unitOfWork = unitOfWork;
#if (UseCaching)
        _cache = cache;
#endif
    }

    public async Task<Result<TodoListResponse>> GetListAsync(
        TodoListRequest request,
        CancellationToken cancellationToken = default)
    {
        const int maxPageSize = 100;

        if (request.PageSize <= 0)
        {
            return Result.Failure<TodoListResponse>(DomainErrors.Pagination.InvalidPageSize(request.PageSize));
        }

        if (request.PageIndex <= 0)
        {
            return Result.Failure<TodoListResponse>(DomainErrors.Pagination.InvalidPageIndex(request.PageIndex));
        }

        if (request.PageSize > maxPageSize)
        {
            return Result.Failure<TodoListResponse>(DomainErrors.Pagination.PageSizeTooLarge(request.PageSize, maxPageSize));
        }

        var filterSpec = new TodoFilterSpecification(
            request.SearchTerm,
            request.IsCompleted,
            request.DueBefore,
            request.DueAfter,
            request.Priority);

        var totalCount = await _todoRepository.CountAsync(filterSpec.Criteria, cancellationToken);

        var pagedSpec = new TodoPaginatedSpecification(
            request.PageSize,
            request.PageIndex,
            filterSpec);

        var todos = await _todoRepository.GetAsync(pagedSpec, cancellationToken);
        var items = todos.Select(MapToResponse).ToList();

        var response = new TodoListResponse(
            items,
            totalCount,
            request.PageIndex,
            request.PageSize);

        return Result.Success(response);
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
            todo = await _todoRepository.GetFirstAsync(new TodoByIdSpecification(id), cancellationToken);
            if (todo is not null)
            {
                _cache.Set(GetTodoCacheKey(id), todo, TimeSpan.FromMinutes(5));
            }
        }
#else
        todo = await _todoRepository.GetFirstAsync(new TodoByIdSpecification(id), cancellationToken);
#endif
        return todo is null
            ? Result.Failure<TodoResponse>(DomainErrors.General.NotFound(nameof(Todo), id))
            : Result.Success(MapToResponse(todo));
    }

    public async Task<Result<TodoResponse>> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var todo = new Todo(request.Title, request.Description, request.Priority, request.DueDate);
        await _todoRepository.AddAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif
        return Result.Success(MapToResponse(todo));
    }

    public async Task<Result<TodoResponse>> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var todo = await _todoRepository.GetByIdAsync(id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure<TodoResponse>(DomainErrors.General.NotFound(nameof(Todo), id));
        }

        todo.Update(request.Title, request.Description, request.Priority, request.DueDate);
        await _todoRepository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif
        return Result.Success(MapToResponse(todo));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var todo = await _todoRepository.GetByIdAsync(id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure(DomainErrors.General.NotFound(nameof(Todo), id));
        }

        todo.Delete();
        await _todoRepository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif
        return Result.Success();
    }

    public async Task<Result> CompleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var todo = await _todoRepository.GetByIdAsync(id, cancellationToken);
        if (todo is null)
        {
            return Result.Failure(DomainErrors.General.NotFound(nameof(Todo), id));
        }

        todo.MarkAsCompleted();
        await _todoRepository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
#if (UseCaching)
        InvalidateCache(todo.Id);
#endif
        return Result.Success();
    }

    private static TodoResponse MapToResponse(Todo todo) =>
        new(todo.Id, todo.Title, todo.Description, todo.IsCompleted, todo.Priority, todo.DueDate);

#if (UseCaching)
    private static string GetTodoCacheKey(int id) => $"todo_{id}";

    private void InvalidateCache(int id)
    {
        _cache.Remove(GetTodoCacheKey(id));
    }
#endif
}
