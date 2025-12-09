namespace MCA.Application.DTOs;

public record TodoResponse(int Id, string Title, string? Description, bool IsCompleted, int Priority, DateTime? DueDate);

public record CreateTodoRequest(string Title, string? Description, int Priority = 0, DateTime? DueDate = null);

public record UpdateTodoRequest(string Title, string? Description, int Priority = 0, DateTime? DueDate = null);

public record TodoListRequest(
    string? SearchTerm = null,
    bool? IsCompleted = null,
    int? Priority = null,
    DateTime? DueBefore = null,
    DateTime? DueAfter = null,
    int PageIndex = 1,
    int PageSize = 10);

public record TodoListResponse(
    IReadOnlyList<TodoResponse> Items,
    int TotalCount,
    int PageIndex,
    int PageSize);
