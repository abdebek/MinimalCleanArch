using MCA.Application.DTOs;
using MCA.Application.Services;
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MinimalCleanArch.Common;
using MinimalCleanArch.Repositories;

namespace MCA.Infrastructure.Services;

/// <summary>
/// Service implementation for Todo operations.
/// </summary>
public class TodoService : ITodoService
{
    private readonly ITodoRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TodoService(ITodoRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TodoResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var todo = await _repository.GetByIdAsync(id, cancellationToken);

        if (todo is null)
        {
            return Result<TodoResponse>.Failure(Error.NotFound("Todo.NotFound", $"Todo with id {id} not found"));
        }

        return Result<TodoResponse>.Success(MapToResponse(todo));
    }

    public async Task<Result<IReadOnlyList<TodoResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var todos = await _repository.GetAllAsync(cancellationToken);
        var response = todos.Select(MapToResponse).ToList();
        return Result<IReadOnlyList<TodoResponse>>.Success(response);
    }

    public async Task<Result<TodoResponse>> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var todo = new Todo(request.Title, request.Description, request.Priority, request.DueDate);

        await _repository.AddAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TodoResponse>.Success(MapToResponse(todo));
    }

    public async Task<Result<TodoResponse>> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var todo = await _repository.GetByIdAsync(id, cancellationToken);

        if (todo is null)
        {
            return Result<TodoResponse>.Failure(Error.NotFound("Todo.NotFound", $"Todo with id {id} not found"));
        }

        todo.Update(request.Title, request.Description, request.Priority, request.DueDate);

        await _repository.UpdateAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TodoResponse>.Success(MapToResponse(todo));
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
            todo.UpdatedAt);
    }
}
