#if (UseControllers)
using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Application.Handlers;
using Microsoft.AspNetCore.Mvc;
using MinimalCleanArch.Domain.Common;
#if (UseMessaging)
using Wolverine;
#endif
#if (UseAuth)
using MCA.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
#endif

namespace MCA.Api.Controllers;

/// <summary>
/// Second host adapter: ASP.NET controllers using the same Todo handlers as Minimal APIs.
/// </summary>
[ApiController]
[Route("api/todos")]
#if (UseAuth && UseMultiTenant)
[Authorize]
#endif
public class TodoController : ControllerBase
{
#if (UseMessaging)
    private readonly IMessageBus _bus;

    public TodoController(IMessageBus bus) => _bus = bus;
#else
    private readonly TodoCommandHandler _handler;

    public TodoController(TodoCommandHandler handler) => _handler = handler;
#endif

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoListResult>>(new GetAllTodosQuery(), cancellationToken);
#else
        var result = await _handler.Handle(new GetAllTodosQuery(), cancellationToken);
#endif
        return ToAction(result, value => Ok(value.Items));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoResponse>>(new GetTodoByIdQuery(id), cancellationToken);
#else
        var result = await _handler.Handle(new GetTodoByIdQuery(id), cancellationToken);
#endif
        return ToAction(result, value => Ok(value));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTodoCommand(request.Title, request.Description, request.Priority, request.DueDate);
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoResponse>>(command, cancellationToken);
#else
        var result = await _handler.Handle(command, cancellationToken);
#endif
        return ToAction(result, value => Created($"/api/todos/{value.Id}", value));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTodoCommand(id, request.Title, request.Description, request.Priority, request.DueDate);
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoResponse>>(command, cancellationToken);
#else
        var result = await _handler.Handle(command, cancellationToken);
#endif
        return ToAction(result, value => Ok(value));
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken)
    {
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result>(new CompleteTodoCommand(id), cancellationToken);
#else
        var result = await _handler.Handle(new CompleteTodoCommand(id), cancellationToken);
#endif
        return ToAction(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result>(new DeleteTodoCommand(id), cancellationToken);
#else
        var result = await _handler.Handle(new DeleteTodoCommand(id), cancellationToken);
#endif
        return ToAction(result);
    }

    [HttpPost("{id:int}/restore")]
#if (UseAuth)
    [Authorize(Roles = Roles.Admin)]
#endif
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoResponse>>(new RestoreTodoCommand(id), cancellationToken);
#else
        var result = await _handler.Handle(new RestoreTodoCommand(id), cancellationToken);
#endif
        return ToAction(result, value => Ok(value));
    }

    private IActionResult ToAction<T>(Result<T> result, Func<T, IActionResult> ok) =>
        result.Match(ok, error => StatusCode(error.StatusCode, new { error.Code, error.Message }));

    private IActionResult ToAction(Result result) =>
        result.Match<IActionResult>(() => NoContent(), error => StatusCode(error.StatusCode, new { error.Code, error.Message }));
}
#endif
