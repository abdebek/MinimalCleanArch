using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Extensions.Realtime;
using MinimalCleanArch.Realtime;

namespace MinimalCleanArch.UnitTests.Realtime;

public class RealtimePortSurfaceTests
{
    [Fact]
    public void PortTypes_AreNotSignalR()
    {
        var portAssembly = typeof(IRealtimePublisher).Assembly;
        portAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Should()
            .NotContain(n => n != null && n.Contains("SignalR", StringComparison.OrdinalIgnoreCase));

        typeof(IRealtimePublisher).GetMethods()
            .SelectMany(m => m.GetParameters())
            .Select(p => p.ParameterType)
            .Concat([typeof(RealtimeMessage)])
            .Select(t => t.Namespace ?? string.Empty)
            .Should()
            .OnlyContain(ns => !ns.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void AddRealtime_RegistersNoOp()
    {
        var services = new ServiceCollection();
        services.AddRealtime();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IRealtimePublisher>().Should().BeOfType<NoOpRealtimePublisher>();
    }
}

public class SignalRRealtimePublisherTests
{
    [Fact]
    public async Task Publish_AfterTodoWrite_ClientReceivesEvent()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddMinimalCleanArchRealtime();
        await using var app = builder.Build();
        app.MapMinimalCleanArchRealtime();
        await app.StartAsync();

        var baseUrl = app.Urls.Single();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = new HubConnectionBuilder()
            .WithUrl(baseUrl.TrimEnd('/') + RealtimeHub.Path)
            .Build();
        connection.On<object>(RealtimeHub.ClientMethod, payload =>
        {
            received.TrySetResult(JsonSerializer.Serialize(payload));
        });
        await connection.StartAsync();
        await connection.InvokeAsync(nameof(RealtimeHub.Subscribe), "todos");

        var publisher = app.Services.GetRequiredService<IRealtimePublisher>();
        publisher.Should().BeOfType<SignalRRealtimePublisher>();
        await publisher.PublishAsync(new RealtimeMessage
        {
            Channel = "todos",
            Payload = new { id = 1, title = "live", action = "created" }
        });

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        completed.Should().Be(received.Task, "SignalR client should receive the Todo write event");
        (await received.Task).Should().Contain("created");
        (await received.Task).Should().Contain("todos");
    }
}
