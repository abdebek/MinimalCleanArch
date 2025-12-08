namespace MCA.Application.DTOs;

public record CreateTodoRequest(
    string Title,
    string? Description = null,
    int Priority = 0,
    DateTime? DueDate = null);

public record UpdateTodoRequest(
    string Title,
    string? Description,
    int Priority,
    DateTime? DueDate);

public record TodoResponse(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    int Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
