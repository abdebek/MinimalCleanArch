using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.Email;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IEmailSender"/> from the <c>EmailSettings</c> section.
    /// Set <c>EmailSettings:Provider</c> to <c>Smtp</c> (default) or <c>Api</c>.
    /// </summary>
    public static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = EmailOptions.SectionName)
    {
        services
            .AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(
                o => string.Equals(o.Provider, EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(o.Provider, EmailProviders.Api, StringComparison.OrdinalIgnoreCase),
                "EmailSettings:Provider must be either 'Smtp' or 'Api'.")
            .Validate(o => o.TimeoutSeconds > 0, "EmailSettings:TimeoutSeconds must be greater than 0.")
            .ValidateOnStart();

        services.AddHttpClient(HttpApiEmailSender.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
        });

        services.TryAddTransient<SmtpEmailSender>();
        services.TryAddTransient<HttpApiEmailSender>();
        services.TryAddTransient<IEmailSender>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            return string.Equals(options.Provider, EmailProviders.Api, StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<HttpApiEmailSender>()
                : sp.GetRequiredService<SmtpEmailSender>();
        });

        return services;
    }
}
