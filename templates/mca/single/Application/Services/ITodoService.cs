using MCA.Application.DTOs;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Services;

public interface ITodoService
{
    Task<Result<IEnumerable<TodoResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<TodoResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<TodoResponse>> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default);
    Task<Result<TodoResponse>> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<Result> CompleteAsync(int id, CancellationToken cancellationToken = default);
}
