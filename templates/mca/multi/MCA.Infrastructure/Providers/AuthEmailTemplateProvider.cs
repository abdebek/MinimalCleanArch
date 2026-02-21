#if (UseAuth)
using MCA.Application.Interfaces;
using MCA.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace MCA.Infrastructure.Providers;

public class AuthEmailTemplateProvider
{
    private readonly EmailSettings _settings;

    public AuthEmailTemplateProvider(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public EmailMessage CreatePasswordResetEmail(string toEmail, string token, string userId)
    {
        var resetUrl = $"{_settings.AppBaseUrl}/reset-password?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
        var body = $"""
            <p>You requested a password reset for your <strong>{_settings.AppName}</strong> account.</p>
            <p style="margin: 24px 0;">
              <a href="{resetUrl}" style="background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">
                Reset Password
              </a>
            </p>
            <p style="color: #666; font-size: 14px;">If you did not request this, please ignore this email. This link expires in 24 hours.</p>
            <p style="color: #999; font-size: 12px;">If the button doesn't work, copy this URL: {resetUrl}</p>
            """;

        return new EmailMessage
        {
            To = toEmail,
            Subject = $"Reset your {_settings.AppName} password",
            HtmlBody = EmailLayouts.WrapInLayout(_settings.AppName, "Reset Password", body)
        };
    }

    public EmailMessage CreateEmailConfirmationEmail(string toEmail, string token, string userId)
    {
        var confirmUrl = $"{_settings.AppBaseUrl}/confirm-email?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
        var body = $"""
            <p>Thank you for registering with <strong>{_settings.AppName}</strong>. Please confirm your email address to activate your account.</p>
            <p style="margin: 24px 0;">
              <a href="{confirmUrl}" style="background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">
                Confirm Email
              </a>
            </p>
            <p style="color: #666; font-size: 14px;">If you did not create an account, please ignore this email.</p>
            <p style="color: #999; font-size: 12px;">If the button doesn't work, copy this URL: {confirmUrl}</p>
            """;

        return new EmailMessage
        {
            To = toEmail,
            Subject = $"Confirm your {_settings.AppName} email",
            HtmlBody = EmailLayouts.WrapInLayout(_settings.AppName, "Confirm Email", body)
        };
    }
}
#endif
