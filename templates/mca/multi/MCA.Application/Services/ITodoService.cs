using MCA.Application.DTOs;
using MinimalCleanArch.Common;

namespace MCA.Application.Services;

/// <summary>
/// Service interface for Todo operations.
/// </summary>
public interface ITodoService
{
    Task<Result<TodoResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TodoResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<TodoResponse>> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default);
    Task<Result<TodoResponse>> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default);
    Task<Result> CompleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
