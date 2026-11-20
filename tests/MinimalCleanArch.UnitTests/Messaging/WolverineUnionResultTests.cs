using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalCleanArch.Domain.Common;
using MinimalCleanArch.Messaging.Extensions;
using Wolverine;

namespace MinimalCleanArch.UnitTests.Messaging;

public class WolverineUnionResultTests
{
    [Fact]
    public async Task Wolverine_InvokeAsync_SupportsCSharp15UnionResultTypes()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddMinimalCleanArchMessaging(opts =>
        {
            opts.ServiceName = "UnionResultTests";
            opts.HandlerAssemblies.Add(typeof(UnionTestHandler).Assembly);
        });

        using var host = builder.Build();
        await host.StartAsync();

        try
        {
            using var scope = host.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var okResult = await bus.InvokeAsync<Result<UnionTestReply>>(new UnionTestQuery(1));
            var okMessage = okResult switch
            {
                UnionTestReply reply => reply.Message,
                Error error => error.Code
            };
            okMessage.Should().Be("item-1");

            var failResult = await bus.InvokeAsync<Result<UnionTestReply>>(new UnionTestQuery(-1));
            var failMessage = failResult switch
            {
                UnionTestReply reply => reply.Message,
                Error error => error.Code
            };
            failMessage.Should().Be("INVALID_ID");

            var voidOk = await bus.InvokeAsync<Result>(new UnionTestCommand(true));
            var voidOkLabel = voidOk switch
            {
                Success => "ok",
                Error error => error.Code
            };
            voidOkLabel.Should().Be("ok");

            var voidFail = await bus.InvokeAsync<Result>(new UnionTestCommand(false));
            var voidFailLabel = voidFail switch
            {
                Success => "ok",
                Error error => error.Code
            };
            voidFailLabel.Should().Be("COMMAND_FAILED");
        }
        finally
        {
            await host.StopAsync();
        }
    }
}

public sealed record UnionTestQuery(int Id);
public sealed record UnionTestReply(string Message);
public sealed record UnionTestCommand(bool Succeed);

public sealed class UnionTestHandler
{
    public Task<Result<UnionTestReply>> Handle(UnionTestQuery query)
    {
        Result<UnionTestReply> result = query.Id > 0
            ? new UnionTestReply($"item-{query.Id}")
            : Error.Validation("INVALID_ID", "Id must be positive");

        return Task.FromResult(result);
    }

    public Task<Result> Handle(UnionTestCommand command)
    {
        Result result = command.Succeed
            ? Result.Success()
            : Error.Failure("COMMAND_FAILED", "Command failed");

        return Task.FromResult(result);
    }
}
