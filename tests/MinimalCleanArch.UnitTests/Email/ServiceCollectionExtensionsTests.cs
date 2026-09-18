using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MinimalCleanArch.Email;

namespace MinimalCleanArch.UnitTests.Email;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEmail_Smtp_RegistersSmtpSender()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:Provider"] = "Smtp",
                ["EmailSettings:SmtpServer"] = "localhost",
                ["EmailSettings:Port"] = "25",
                ["EmailSettings:SenderEmail"] = "no-reply@example.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmail(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEmailSender>().Should().BeOfType<SmtpEmailSender>();
        provider.GetRequiredService<IOptions<EmailOptions>>().Value.SmtpServer.Should().Be("localhost");
    }

    [Fact]
    public void AddEmail_Api_RegistersHttpApiSender()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:Provider"] = "Api",
                ["EmailSettings:Api:Endpoint"] = "https://email.example/send",
                ["EmailSettings:SenderEmail"] = "no-reply@example.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmail(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEmailSender>().Should().BeOfType<HttpApiEmailSender>();
    }
}

public class HttpApiEmailSenderTests
{
    [Fact]
    public async Task SendAsync_PostsJson_AndSetsCorrelationHeader()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var handler = new StubHandler(req =>
        {
            captured = req;
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        });

        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory>(new StubFactory(handler));
        services.AddSingleton<IOptions<EmailOptions>>(Options.Create(new EmailOptions
        {
            SenderEmail = "from@example.com",
            SenderName = "App",
            Api = { Endpoint = "https://email.example/send", ApiKey = "k", ApiKeyPrefix = "Bearer" }
        }));
        services.AddSingleton<ILogger<HttpApiEmailSender>>(NullLogger<HttpApiEmailSender>.Instance);
        services.AddTransient<HttpApiEmailSender>();

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<HttpApiEmailSender>();

        await sender.SendAsync(new EmailMessage
        {
            To = "user@example.com",
            Subject = "Hello",
            HtmlBody = "<p>Hi</p>",
            TextBody = "Hi",
            CorrelationId = "corr-1"
        });

        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Post);
        captured.RequestUri.Should().Be(new Uri("https://email.example/send"));
        captured.Headers.GetValues("X-Correlation-ID").Should().Contain("corr-1");
        captured.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedBody.Should().Contain("user@example.com");
        capturedBody.Should().Contain("Hello");
    }

    [Fact]
    public async Task SendAsync_NonSuccess_Throws()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
        {
            Content = new StringContent("nope")
        });

        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory>(new StubFactory(handler));
        services.AddSingleton<IOptions<EmailOptions>>(Options.Create(new EmailOptions
        {
            SenderEmail = "from@example.com",
            Api = { Endpoint = "https://email.example/send" }
        }));
        services.AddSingleton<ILogger<HttpApiEmailSender>>(NullLogger<HttpApiEmailSender>.Instance);
        services.AddTransient<HttpApiEmailSender>();

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<HttpApiEmailSender>();

        var act = () => sender.SendAsync(new EmailMessage { To = "a@b.c", Subject = "s", HtmlBody = "h" });
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*400*");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_respond(request));
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}

public class EmailPortSurfaceTests
{
    [Fact]
    public void PortTypes_AreNotAspNet()
    {
        typeof(IEmailSender).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Should()
            .NotContain(n => n != null && n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }
}
