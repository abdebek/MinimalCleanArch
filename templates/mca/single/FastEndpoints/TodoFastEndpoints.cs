#if (UseFastEndpoints)
using FastEndpoints;
using MCA.Application.Commands;
using MCA.Application.DTOs;
using MCA.Application.Handlers;
using MinimalCleanArch.Domain.Common;
#if (UseMessaging)
using Wolverine;
#endif
#if (UseAuth)
using MCA.Domain.Constants;
#endif

namespace MCA.FastEndpoints;

/// <summary>
/// Third host adapter: FastEndpoints using the same Todo handlers as Minimal APIs.
/// </summary>
public sealed class GetTodosEndpoint : EndpointWithoutRequest<List<TodoResponse>>
{
#if (UseMessaging)
    private readonly IMessageBus _bus;
    public GetTodosEndpoint(IMessageBus bus) => _bus = bus;
#else
    private readonly TodoCommandHandler _handler;
    public GetTodosEndpoint(TodoCommandHandler handler) => _handler = handler;
#endif

    public override void Configure()
    {
        Get("/api/todos");
        Tags("Todos");
#if (UseAuth && UseMultiTenant)
#else
        AllowAnonymous();
#endif
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoListResult>>(new GetAllTodosQuery(), ct);
#else
        var result = await _handler.Handle(new GetAllTodosQuery(), ct);
#endif
        if (result.IsFailure)
        {
            await FastEndpointErrors.Write(HttpContext, result.Error, ct);
            return;
        }

        await Send.OkAsync(result.Value.Items.ToList(), ct);
    }
}

public sealed class GetTodoEndpoint : EndpointWithoutRequest<TodoResponse>
{
#if (UseMessaging)
    private readonly IMessageBus _bus;
    public GetTodoEndpoint(IMessageBus bus) => _bus = bus;
#else
    private readonly TodoCommandHandler _handler;
    public GetTodoEndpoint(TodoCommandHandler handler) => _handler = handler;
#endif

    public override void Configure()
    {
        Get("/api/todos/{id}");
        Tags("Todos");
#if (UseAuth && UseMultiTenant)
#else
        AllowAnonymous();
#endif
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<int>("id");
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoResponse>>(new GetTodoByIdQuery(id), ct);
#else
        var result = await _handler.Handle(new GetTodoByIdQuery(id), ct);
#endif
        if (result.IsFailure)
        {
            await FastEndpointErrors.Write(HttpContext, result.Error, ct);
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class CreateTodoEndpoint : Endpoint<CreateTodoRequest, TodoResponse>
{
#if (UseMessaging)
    private readonly IMessageBus _bus;
    public CreateTodoEndpoint(IMessageBus bus) => _bus = bus;
#else
    private readonly TodoCommandHandler _handler;
    public CreateTodoEndpoint(TodoCommandHandler handler) => _handler = handler;
#endif

    public override void Configure()
    {
        Post("/api/todos");
        Tags("Todos");
#if (UseAuth && UseMultiTenant)
#else
        AllowAnonymous();
#endif
    }

    public override async Task HandleAsync(CreateTodoRequest req, CancellationToken ct)
    {
        var command = new CreateTodoCommand(req.Title, req.Description, req.Priority, req.DueDate);
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoResponse>>(command, ct);
#else
        var result = await _handler.Handle(command, ct);
#endif
        if (result.IsFailure)
        {
            await FastEndpointErrors.Write(HttpContext, result.Error, ct);
            return;
        }

        HttpContext.Response.Headers.Location = $"/api/todos/{result.Value.Id}";
        await Send.ResponseAsync(result.Value, 201, ct);
    }
}

public sealed class UpdateTodoEndpoint : Endpoint<UpdateTodoRequest, TodoResponse>
{
#if (UseMessaging)
    private readonly IMessageBus _bus;
    public UpdateTodoEndpoint(IMessageBus bus) => _bus = bus;
#else
    private readonly TodoCommandHandler _handler;
    public UpdateTodoEndpoint(TodoCommandHandler handler) => _handler = handler;
#endif

    public override void Configure()
    {
        Put("/api/todos/{id}");
        Tags("Todos");
#if (UseAuth && UseMultiTenant)
#else
        AllowAnonymous();
#endif
    }

    public override async Task HandleAsync(UpdateTodoRequest req, CancellationToken ct)
    {
        var id = Route<int>("id");
        var command = new UpdateTodoCommand(id, req.Title, req.Description, req.Priority, req.DueDate);
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoResponse>>(command, ct);
#else
        var result = await _handler.Handle(command, ct);
#endif
        if (result.IsFailure)
        {
            await FastEndpointErrors.Write(HttpContext, result.Error, ct);
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class CompleteTodoEndpoint : EndpointWithoutRequest
{
#if (UseMessaging)
    private readonly IMessageBus _bus;
    public CompleteTodoEndpoint(IMessageBus bus) => _bus = bus;
#else
    private readonly TodoCommandHandler _handler;
    public CompleteTodoEndpoint(TodoCommandHandler handler) => _handler = handler;
#endif

    public override void Configure()
    {
        Post("/api/todos/{id}/complete");
        Tags("Todos");
#if (UseAuth && UseMultiTenant)
#else
        AllowAnonymous();
#endif
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<int>("id");
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result>(new CompleteTodoCommand(id), ct);
#else
        var result = await _handler.Handle(new CompleteTodoCommand(id), ct);
#endif
        if (result.IsFailure)
        {
            await FastEndpointErrors.Write(HttpContext, result.Error, ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public sealed class DeleteTodoEndpoint : EndpointWithoutRequest
{
#if (UseMessaging)
    private readonly IMessageBus _bus;
    public DeleteTodoEndpoint(IMessageBus bus) => _bus = bus;
#else
    private readonly TodoCommandHandler _handler;
    public DeleteTodoEndpoint(TodoCommandHandler handler) => _handler = handler;
#endif

    public override void Configure()
    {
        Delete("/api/todos/{id}");
        Tags("Todos");
#if (UseAuth && UseMultiTenant)
#else
        AllowAnonymous();
#endif
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<int>("id");
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result>(new DeleteTodoCommand(id), ct);
#else
        var result = await _handler.Handle(new DeleteTodoCommand(id), ct);
#endif
        if (result.IsFailure)
        {
            await FastEndpointErrors.Write(HttpContext, result.Error, ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public sealed class RestoreTodoEndpoint : EndpointWithoutRequest<TodoResponse>
{
#if (UseMessaging)
    private readonly IMessageBus _bus;
    public RestoreTodoEndpoint(IMessageBus bus) => _bus = bus;
#else
    private readonly TodoCommandHandler _handler;
    public RestoreTodoEndpoint(TodoCommandHandler handler) => _handler = handler;
#endif

    public override void Configure()
    {
        Post("/api/todos/{id}/restore");
        Tags("Todos");
#if (UseAuth)
        Roles(Roles.Admin);
#else
        AllowAnonymous();
#endif
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<int>("id");
#if (UseMessaging)
        var result = await _bus.InvokeAsync<Result<TodoResponse>>(new RestoreTodoCommand(id), ct);
#else
        var result = await _handler.Handle(new RestoreTodoCommand(id), ct);
#endif
        if (result.IsFailure)
        {
            await FastEndpointErrors.Write(HttpContext, result.Error, ct);
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

internal static class FastEndpointErrors
{
    public static Task Write(HttpContext http, Error error, CancellationToken ct)
    {
        http.Response.StatusCode = error.StatusCode;
        return http.Response.WriteAsJsonAsync(new { error.Code, error.Message }, ct);
    }
}
#endif
