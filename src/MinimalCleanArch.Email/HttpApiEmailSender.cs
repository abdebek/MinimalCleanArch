using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.Email;

/// <summary>
/// HTTP email transport (Cloudflare Email Sending and similar REST APIs).
/// Uses camelCase JSON and address objects with <c>address</c>/<c>name</c>.
/// </summary>
public sealed class HttpApiEmailSender : IEmailSender
{
    public const string HttpClientName = "McaEmailApi";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly EmailOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpApiEmailSender> _logger;

    public HttpApiEmailSender(
        IOptions<EmailOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpApiEmailSender> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Api.Endpoint))
        {
            throw new InvalidOperationException("EmailSettings:Api:Endpoint is required when Provider is Api.");
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Api.Endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        AddAuthenticationHeader(request.Headers);
        AddCustomHeaders(request.Headers);

        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", message.CorrelationId);
        }

        var payload = new ApiEmailPayload
        {
            From = new ApiEmailAddress
            {
                Address = _options.SenderEmail,
                Name = _options.SenderName
            },
            To = message.To,
            Subject = message.Subject,
            Html = message.HtmlBody,
            Text = message.TextBody
        };

        request.Content = JsonContent.Create(payload, options: JsonOptions);

        _logger.LogDebug("Sending API email to {To}: {Subject}", message.To, message.Subject);

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var summary = responseBody.Length <= 500 ? responseBody : responseBody[..500] + "...";

        throw new InvalidOperationException(
            $"Email API request failed with status {(int)response.StatusCode} ({response.StatusCode}). Response: {summary}");
    }

    private void AddAuthenticationHeader(HttpRequestHeaders headers)
    {
        if (string.IsNullOrWhiteSpace(_options.Api.ApiKey))
        {
            return;
        }

        var headerName = string.IsNullOrWhiteSpace(_options.Api.ApiKeyHeaderName)
            ? "Authorization"
            : _options.Api.ApiKeyHeaderName;

        var headerValue = string.IsNullOrWhiteSpace(_options.Api.ApiKeyPrefix)
            ? _options.Api.ApiKey
            : $"{_options.Api.ApiKeyPrefix} {_options.Api.ApiKey}";

        headers.TryAddWithoutValidation(headerName, headerValue);
    }

    private void AddCustomHeaders(HttpRequestHeaders headers)
    {
        if (_options.Api.Headers.Count == 0)
        {
            return;
        }

        foreach (var header in _options.Api.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key) || string.IsNullOrWhiteSpace(header.Value))
            {
                continue;
            }

            headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private sealed class ApiEmailPayload
    {
        public ApiEmailAddress From { get; set; } = new();
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public string? Text { get; set; }
    }

    private sealed class ApiEmailAddress
    {
        public string Address { get; set; } = string.Empty;
        public string? Name { get; set; }
    }
}
