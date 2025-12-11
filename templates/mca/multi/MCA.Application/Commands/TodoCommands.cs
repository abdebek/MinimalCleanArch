using MCA.Application.DTOs;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Commands;

public record GetAllTodosQuery;

public record GetTodoByIdQuery(int Id);

public record CreateTodoCommand(string Title, string? Description = null, int Priority = 0, DateTime? DueDate = null);

public record UpdateTodoCommand(int Id, string Title, string? Description = null, int Priority = 0, DateTime? DueDate = null);

public record CompleteTodoCommand(int Id);

public record DeleteTodoCommand(int Id);

public record TodoListResult(IReadOnlyList<TodoResponse> Items);
