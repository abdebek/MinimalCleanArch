using Microsoft.AspNetCore.Http;

namespace MinimalCleanArch.Extensions.Middlewares;

/// <summary>
/// Middleware that adds security headers to HTTP responses.
/// Implements common security best practices including CSP, HSTS, and various X-* headers.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">Security headers configuration options.</param>
    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions? options = null)
    {
        _next = next;
        _options = options ?? new SecurityHeadersOptions();
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before the response starts
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME type sniffing
            if (_options.EnableXContentTypeOptions)
            {
                headers["X-Content-Type-Options"] = "nosniff";
            }

            // Prevent clickjacking
            if (_options.EnableXFrameOptions)
            {
                headers["X-Frame-Options"] = _options.XFrameOptionsValue;
            }

            // Enable XSS filter in older browsers
            if (_options.EnableXXssProtection)
            {
                headers["X-XSS-Protection"] = "1; mode=block";
            }

            // Referrer policy
            if (_options.EnableReferrerPolicy)
            {
                headers["Referrer-Policy"] = _options.ReferrerPolicyValue;
            }

            // Content Security Policy
            if (_options.EnableContentSecurityPolicy && !string.IsNullOrEmpty(_options.ContentSecurityPolicy))
            {
                headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            }

            // Permissions Policy (formerly Feature-Policy)
            if (_options.EnablePermissionsPolicy && !string.IsNullOrEmpty(_options.PermissionsPolicy))
            {
                headers["Permissions-Policy"] = _options.PermissionsPolicy;
            }

            // Strict Transport Security (HSTS)
            if (_options.EnableHsts && context.Request.IsHttps)
            {
                var hstsValue = $"max-age={_options.HstsMaxAgeSeconds}";
                if (_options.HstsIncludeSubDomains)
                {
                    hstsValue += "; includeSubDomains";
                }
                if (_options.HstsPreload)
                {
                    hstsValue += "; preload";
                }
                headers["Strict-Transport-Security"] = hstsValue;
            }

            // Remove server header
            if (_options.RemoveServerHeader)
            {
                headers.Remove("Server");
                headers.Remove("X-Powered-By");
            }

            // Cache control for sensitive pages
            if (_options.EnableNoCacheForAuthenticated &&
                context.User.Identity?.IsAuthenticated == true)
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Configuration options for security headers middleware.
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// Gets or sets whether to add X-Content-Type-Options header. Default: true.
    /// </summary>
    public bool EnableXContentTypeOptions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add X-Frame-Options header. Default: true.
    /// </summary>
    public bool EnableXFrameOptions { get; set; } = true;

    /// <summary>
    /// Gets or sets the X-Frame-Options value. Default: "DENY".
    /// </summary>
    public string XFrameOptionsValue { get; set; } = "DENY";

    /// <summary>
    /// Gets or sets whether to add X-XSS-Protection header. Default: true.
    /// </summary>
    public bool EnableXXssProtection { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add Referrer-Policy header. Default: true.
    /// </summary>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>
    /// Gets or sets the Referrer-Policy value. Default: "strict-origin-when-cross-origin".
    /// </summary>
    public string ReferrerPolicyValue { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Gets or sets whether to add Content-Security-Policy header. Default: false.
    /// </summary>
    public bool EnableContentSecurityPolicy { get; set; } = false;

    /// <summary>
    /// Gets or sets the Content-Security-Policy value.
    /// </summary>
    public string? ContentSecurityPolicy { get; set; }

    /// <summary>
    /// Gets or sets whether to add Permissions-Policy header. Default: false.
    /// </summary>
    public bool EnablePermissionsPolicy { get; set; } = false;

    /// <summary>
    /// Gets or sets the Permissions-Policy value.
    /// </summary>
    public string? PermissionsPolicy { get; set; }

    /// <summary>
    /// Gets or sets whether to add Strict-Transport-Security (HSTS) header. Default: true.
    /// </summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// Gets or sets the HSTS max-age in seconds. Default: 31536000 (1 year).
    /// </summary>
    public int HstsMaxAgeSeconds { get; set; } = 31536000;

    /// <summary>
    /// Gets or sets whether to include subdomains in HSTS. Default: true.
    /// </summary>
    public bool HstsIncludeSubDomains { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add preload directive to HSTS. Default: false.
    /// </summary>
    public bool HstsPreload { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to remove Server and X-Powered-By headers. Default: true.
    /// </summary>
    public bool RemoveServerHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add no-cache headers for authenticated requests. Default: true.
    /// </summary>
    public bool EnableNoCacheForAuthenticated { get; set; } = true;

    /// <summary>
    /// Creates options configured for API-only applications.
    /// </summary>
    public static SecurityHeadersOptions ForApi() => new()
    {
        EnableXFrameOptions = true,
        XFrameOptionsValue = "DENY",
        EnableContentSecurityPolicy = true,
        ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'",
        EnablePermissionsPolicy = true,
        PermissionsPolicy = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
    };

    /// <summary>
    /// Creates options configured for web applications with moderate CSP.
    /// </summary>
    public static SecurityHeadersOptions ForWebApp() => new()
    {
        EnableXFrameOptions = true,
        XFrameOptionsValue = "SAMEORIGIN",
        EnableContentSecurityPolicy = true,
        ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; frame-ancestors 'self'",
        EnablePermissionsPolicy = true,
        PermissionsPolicy = "camera=(), microphone=(), geolocation=()"
    };
}
