using MCA.Application.Interfaces;
using MCA.Domain.Constants;
using MCA.Domain.Entities;
using MCA.Infrastructure.Configuration;
using MCA.Infrastructure.Data;
using MCA.Infrastructure.Providers;
using MCA.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using System.Net.Mail;

namespace MCA.Api.Configuration;

public static class IdentityServiceExtensions
{
    private const string DefaultWebClientId = "mca-web-client";
    private const string DefaultWebClientSecret = "mca-default-secret-change-me";

    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment = false)
    {
        services.AddOptions<OpenIddictSettings>()
            .Bind(configuration.GetSection(OpenIddictSettings.SectionName))
            .ValidateOnStart();

        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection(EmailSettings.SectionName))
            .Validate(settings => IsSupportedEmailProvider(settings.Provider), "EmailSettings:Provider must be either 'Smtp' or 'Api'.")
            .Validate(settings => settings.TimeoutSeconds > 0, "EmailSettings:TimeoutSeconds must be greater than 0.")
            .Validate(settings => IsValidEmail(settings.SenderEmail), "EmailSettings:SenderEmail must be a valid email address.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.AppBaseUrl) && Uri.TryCreate(settings.AppBaseUrl, UriKind.Absolute, out _), "EmailSettings:AppBaseUrl must be an absolute URI.")
            .Validate(settings => !IsSmtpProvider(settings.Provider) || !string.IsNullOrWhiteSpace(settings.SmtpServer), "EmailSettings:SmtpServer is required when Provider is Smtp.")
            .Validate(settings => !IsSmtpProvider(settings.Provider) || settings.Port is > 0 and <= 65535, "EmailSettings:Port must be between 1 and 65535 when Provider is Smtp.")
            .Validate(settings => !IsSmtpProvider(settings.Provider) || isDevelopment || settings.EnableSsl, "EmailSettings:EnableSsl must be true outside development when Provider is Smtp.")
            .Validate(
                settings => !IsApiProvider(settings.Provider) ||
                    (settings.Api is not null &&
                     !string.IsNullOrWhiteSpace(settings.Api.Endpoint) &&
                     Uri.TryCreate(settings.Api.Endpoint, UriKind.Absolute, out _)),
                "EmailSettings:Api:Endpoint must be an absolute URI when Provider is Api.")
            .Validate(
                settings => !IsApiProvider(settings.Provider) ||
                    settings.Api is null ||
                    string.IsNullOrWhiteSpace(settings.Api.ApiKey) ||
                    !string.IsNullOrWhiteSpace(settings.Api.ApiKeyHeaderName),
                "EmailSettings:Api:ApiKeyHeaderName is required when ApiKey is configured.")
            .ValidateOnStart();

        var oidcSettings = configuration.GetSection(OpenIddictSettings.SectionName).Get<OpenIddictSettings>()
            ?? new OpenIddictSettings();

        if (!isDevelopment)
        {
            ValidateProductionOpenIddictSettings(oidcSettings);
        }

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            options.Lockout.MaxFailedAccessAttempts = 5;

            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@";

            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 403;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        })
        // External provider cookie (used during external OAuth callback)
        .AddCookie("ExternalCookie", options =>
        {
            options.Cookie.Name = "MCA.External";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });
        // External providers (uncomment and add NuGet packages to enable):
        // .AddGoogle(options =>
        // {
        //     options.ClientId = configuration["Authentication:Google:ClientId"]!;
        //     options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
        //     options.SignInScheme = "ExternalCookie";
        // })
        // .AddMicrosoftAccount(options =>
        // {
        //     options.ClientId = configuration["Authentication:Microsoft:ClientId"]!;
        //     options.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"]!;
        //     options.SignInScheme = "ExternalCookie";
        // });
        // GitHub: install AspNet.Security.OAuth.GitHub
        // .AddGitHub(options =>
        // {
        //     options.ClientId = configuration["Authentication:GitHub:ClientId"]!;
        //     options.ClientSecret = configuration["Authentication:GitHub:ClientSecret"]!;
        //     options.Scope.Add("user:email"); // Needed when GitHub email is private
        //     options.SignInScheme = "ExternalCookie";
        // });

        services.AddAuthorization(options =>
        {
            var openIddictPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy("OpenIddict", openIddictPolicy);
            options.DefaultPolicy = openIddictPolicy;
        });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AppDbContext>()
                       .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetEndSessionEndpointUris("/connect/logout")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserInfoEndpointUris("/connect/userinfo")
                       .SetIntrospectionEndpointUris("/connect/introspect")
                       .SetRevocationEndpointUris("/connect/revoke");

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles,
                    OpenIddictConstants.Scopes.OfflineAccess,
                    "mca.api");

                options.AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow()
                       .AllowPasswordFlow()
                       .AllowClientCredentialsFlow();

                options.UseReferenceAccessTokens()
                       .UseReferenceRefreshTokens();

                if (isDevelopment)
                {
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                    options.DisableAccessTokenEncryption();
                }
                else
                {
                    var signingCert = CertificateLoader.Load(oidcSettings.SigningCertificate);
                    var encryptionCert = CertificateLoader.Load(oidcSettings.EncryptionCertificate);

                    if (signingCert == null)
                        throw new InvalidOperationException("OpenIddict signing certificate could not be loaded.");
                    options.AddSigningCertificate(signingCert);

                    if (encryptionCert == null)
                        throw new InvalidOperationException("OpenIddict encryption certificate could not be loaded.");
                    options.AddEncryptionCertificate(encryptionCert);
                }

                var aspNetCoreBuilder = options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableTokenEndpointPassthrough();

                if (isDevelopment)
                {
                    aspNetCoreBuilder.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>, ConfigureOpenIddictServerOptions>();

        // Token service
        services.AddScoped<ITokenService, OpenIddictTokenService>();

        // Email services
        services.AddHttpClient("AuthEmailApi", (sp, client) =>
        {
            var emailSettings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, emailSettings.TimeoutSeconds));
        });
        services.AddTransient<AuthEmailTemplateProvider>();
        services.AddTransient<SmtpEmailSender>();
        services.AddTransient<ApiEmailSender>();
        services.AddTransient<IEmailSender>(sp =>
        {
            var emailSettings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;
            return IsApiProvider(emailSettings.Provider)
                ? sp.GetRequiredService<ApiEmailSender>()
                : sp.GetRequiredService<SmtpEmailSender>();
        });
        services.AddTransient<IEmailService, EmailService>();

        // PKCE helper
        services.AddScoped<PkceService>();

        // Session (for OAuth PKCE demo flow)
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.Cookie.Name = "MCA.Session";
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        return services;
    }

    public static async Task SeedOpenIddictApplicationsAsync(
        this IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var settings = configuration.GetSection(OpenIddictSettings.SectionName).Get<OpenIddictSettings>()
            ?? new OpenIddictSettings();

        var redirectBaseUrls = ResolveRedirectBaseUrls(configuration);

        // Web client (authorization code + PKCE)
        var hasWebClient = settings.Clients.TryGetValue("Web", out var webClient);
        var webClientId = hasWebClient && !string.IsNullOrWhiteSpace(webClient!.ClientId)
            ? webClient.ClientId
            : DefaultWebClientId;
        var webSecret = hasWebClient
            ? webClient!.Secret
            : DefaultWebClientSecret;

        var webClientDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = webClientId,
            ClientSecret = webSecret,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "MCA Web Client",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access",
                OpenIddictConstants.Permissions.Prefixes.Scope + "mca.api"
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var baseUrl in redirectBaseUrls)
        {
            webClientDescriptor.RedirectUris.Add(new Uri($"{baseUrl}/oauth/demo/callback"));
            webClientDescriptor.PostLogoutRedirectUris.Add(new Uri($"{baseUrl}/"));
        }

        await UpsertClientAsync(manager, webClientDescriptor);

        // Mobile/SPA client (public, PKCE only, no secret)
        var mobileClientDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "mca-mobile-client",
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "MCA Mobile Client",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            RedirectUris = { new Uri("nativeapp://callback") },
            PostLogoutRedirectUris = { new Uri("nativeapp://logout") },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access",
                OpenIddictConstants.Permissions.Prefixes.Scope + "mca.api"
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var baseUrl in redirectBaseUrls)
        {
            mobileClientDescriptor.RedirectUris.Add(new Uri($"{baseUrl}/oauth/demo/callback"));
        }

        await UpsertClientAsync(manager, mobileClientDescriptor);

        await SeedBootstrapAdminAsync(scope.ServiceProvider, configuration);
    }

    private static async Task UpsertClientAsync(
        IOpenIddictApplicationManager manager,
        OpenIddictApplicationDescriptor descriptor)
    {
        var existing = await manager.FindByClientIdAsync(descriptor.ClientId!);
        if (existing == null)
            await manager.CreateAsync(descriptor);
        else
            await manager.UpdateAsync(existing, descriptor);
    }

    private static async Task SeedBootstrapAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        var enableBootstrapAdmin = configuration.GetValue<bool>("Seed:EnableBootstrapAdmin");
        if (!enableBootstrapAdmin)
            return;

        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        var adminFirstName = configuration["Seed:AdminFirstName"] ?? "System";
        var adminLastName = configuration["Seed:AdminLastName"] ?? "Administrator";
        var adminRole = configuration["Seed:AdminRole"] ?? Roles.Admin;

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Bootstrap admin seeding is enabled but Seed:AdminEmail and/or Seed:AdminPassword is missing.");
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(adminRole));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Failed to create bootstrap admin role '{adminRole}': {FormatIdentityErrors(roleResult.Errors)}");
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser(adminFirstName, adminLastName, adminEmail)
            {
                EmailConfirmed = true
            };

            var createUserResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createUserResult.Succeeded)
                throw new InvalidOperationException($"Failed to create bootstrap admin user '{adminEmail}': {FormatIdentityErrors(createUserResult.Errors)}");
        }
        else if (!adminUser.EmailConfirmed)
        {
            adminUser.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(adminUser);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException($"Failed to update bootstrap admin user '{adminEmail}': {FormatIdentityErrors(updateResult.Errors)}");
        }

        if (!await userManager.IsInRoleAsync(adminUser, adminRole))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminRole);
            if (!addToRoleResult.Succeeded)
                throw new InvalidOperationException($"Failed to assign role '{adminRole}' to bootstrap admin '{adminEmail}': {FormatIdentityErrors(addToRoleResult.Errors)}");
        }
    }

    private static string FormatIdentityErrors(IEnumerable<IdentityError> errors)
    {
        return string.Join("; ", errors.Select(error => error.Description));
    }

    private static IReadOnlyList<string> ResolveRedirectBaseUrls(IConfiguration configuration)
    {
        var results = new List<string>();

        AddBaseUrlCandidate(results, configuration["App:BaseUrl"]);

        var aspNetCoreUrls = configuration["ASPNETCORE_URLS"] ?? configuration["urls"];
        if (!string.IsNullOrWhiteSpace(aspNetCoreUrls))
        {
            var candidates = aspNetCoreUrls.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var candidate in candidates)
                AddBaseUrlCandidate(results, candidate);
        }

        if (results.Count == 0)
            results.Add("https://localhost:5001");

        return results;
    }

    private static void AddBaseUrlCandidate(List<string> results, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var normalized = uri.GetLeftPart(UriPartial.Authority);
        if (!results.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            results.Add(normalized);
    }

    private static void ValidateProductionOpenIddictSettings(OpenIddictSettings settings)
    {
        if (settings.SigningCertificate.Source == CertificateSource.None)
            throw new InvalidOperationException("OpenIddict:SigningCertificate must be configured outside development.");

        if (settings.EncryptionCertificate.Source == CertificateSource.None)
            throw new InvalidOperationException("OpenIddict:EncryptionCertificate must be configured outside development.");

        var hasWebClient = settings.Clients.TryGetValue("Web", out var webClient);
        var secret = webClient?.Secret;
        if (!hasWebClient || string.IsNullOrWhiteSpace(secret) || secret == DefaultWebClientSecret)
            throw new InvalidOperationException("OpenIddict:Clients:Web:Secret must be set to a non-default value outside development.");
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsSupportedEmailProvider(string? provider)
    {
        return IsSmtpProvider(provider) || IsApiProvider(provider);
    }

    private static bool IsSmtpProvider(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider) ||
               string.Equals(provider, EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsApiProvider(string? provider)
    {
        return string.Equals(provider, EmailProviders.Api, StringComparison.OrdinalIgnoreCase);
    }
}
