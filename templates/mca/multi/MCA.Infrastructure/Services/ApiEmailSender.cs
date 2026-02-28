#if (UseAuth)
using MCA.Application.Interfaces;
using MCA.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MCA.Infrastructure.Services;

public class ApiEmailSender : IEmailSender
{
    private const string HttpClientName = "AuthEmailApi";

    private readonly EmailSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiEmailSender> _logger;

    public ApiEmailSender(
        IOptions<EmailSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<ApiEmailSender> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Api.Endpoint))
            throw new InvalidOperationException("EmailSettings:Api:Endpoint is required when Provider is Api.");

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Api.Endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        AddAuthenticationHeader(request.Headers);
        AddCustomHeaders(request.Headers);

        request.Content = JsonContent.Create(new ApiEmailPayload
        {
            From = new ApiEmailAddress
            {
                Email = _settings.SenderEmail,
                Name = _settings.SenderName
            },
            To =
            [
                new ApiEmailAddress { Email = message.To }
            ],
            Subject = message.Subject,
            Html = message.HtmlBody,
            Text = message.TextBody
        });

        _logger.LogDebug("Sending API email to {To}: {Subject}", message.To, message.Subject);

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Content.ReadAsStringAsync();
        var summary = responseBody.Length <= 500 ? responseBody : responseBody[..500] + "...";

        throw new InvalidOperationException(
            $"Email API request failed with status {(int)response.StatusCode} ({response.StatusCode}). Response: {summary}");
    }

    private void AddAuthenticationHeader(HttpRequestHeaders headers)
    {
        if (string.IsNullOrWhiteSpace(_settings.Api.ApiKey))
            return;

        var headerName = string.IsNullOrWhiteSpace(_settings.Api.ApiKeyHeaderName)
            ? "Authorization"
            : _settings.Api.ApiKeyHeaderName;

        var headerValue = string.IsNullOrWhiteSpace(_settings.Api.ApiKeyPrefix)
            ? _settings.Api.ApiKey
            : $"{_settings.Api.ApiKeyPrefix} {_settings.Api.ApiKey}";

        headers.TryAddWithoutValidation(headerName, headerValue);
    }

    private void AddCustomHeaders(HttpRequestHeaders headers)
    {
        if (_settings.Api.Headers.Count == 0)
            return;

        foreach (var header in _settings.Api.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key) || string.IsNullOrWhiteSpace(header.Value))
                continue;

            headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private sealed class ApiEmailPayload
    {
        public ApiEmailAddress From { get; set; } = new();
        public ApiEmailAddress[] To { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public string? Text { get; set; }
    }

    private sealed class ApiEmailAddress
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }
}
#endif
