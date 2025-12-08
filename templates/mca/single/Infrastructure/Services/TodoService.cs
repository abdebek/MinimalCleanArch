using MCA.Application.DTOs;
using MCA.Application.Services;
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MinimalCleanArch.Results;
using MinimalCleanArch.Results.Errors;

namespace MCA.Infrastructure.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TodoService(ITodoRepository todoRepository, IUnitOfWork unitOfWork)
    {
        _todoRepository = todoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<TodoResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var todos = await _todoRepository.GetAllAsync(cancellationToken);
        var response = todos.Select(MapToResponse);
        return Result.Success(response);
    }

    public async Task<Result<TodoResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var todo = await _todoRepository.GetByIdAsync(id, cancellationToken);
        return todo is null
            ? Result.Failure<TodoResponse>(DomainErrors.General.NotFound(nameof(Todo), id))
            : Result.Success(MapToResponse(todo));
    }

    public async Task<Result<TodoResponse>> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var todo = new Todo(request.Title, request.Description, request.Priority, request.DueDate);
        await _todoRepository.AddAsync(todo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        _todoRepository.Update(todo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        _todoRepository.Update(todo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
        _todoRepository.Update(todo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static TodoResponse MapToResponse(Todo todo) =>
        new(todo.Id, todo.Title, todo.Description, todo.IsCompleted, todo.Priority, todo.DueDate);
}
