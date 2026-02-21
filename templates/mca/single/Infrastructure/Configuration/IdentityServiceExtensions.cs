using MCA.Application.Interfaces;
using MCA.Domain.Entities;
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

namespace MCA.Infrastructure.Configuration;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment = false)
    {
        services.Configure<OpenIddictSettings>(configuration.GetSection(OpenIddictSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        var oidcSettings = configuration.GetSection(OpenIddictSettings.SectionName).Get<OpenIddictSettings>()
            ?? new OpenIddictSettings();

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

                    if (signingCert != null)
                        options.AddSigningCertificate(signingCert);
                    else
                        options.AddDevelopmentSigningCertificate();

                    if (encryptionCert != null)
                        options.AddEncryptionCertificate(encryptionCert);
                    else
                        options.AddDevelopmentEncryptionCertificate();
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
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddTransient<AuthEmailTemplateProvider>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
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

        var appBaseUrl = configuration["App:BaseUrl"] ?? "https://localhost:5001";

        // Web client (authorization code + PKCE)
        var webSecret = settings.Clients.TryGetValue("Web", out var webClient)
            ? webClient.Secret
            : "mca-default-secret-change-me";

        await UpsertClientAsync(manager, new OpenIddictApplicationDescriptor
        {
            ClientId = "mca-web-client",
            ClientSecret = webSecret,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "MCA Web Client",
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            RedirectUris = { new Uri($"{appBaseUrl}/oauth/demo/callback") },
            PostLogoutRedirectUris = { new Uri($"{appBaseUrl}/") },
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
        });

        // Mobile/SPA client (public, PKCE only, no secret)
        await UpsertClientAsync(manager, new OpenIddictApplicationDescriptor
        {
            ClientId = "mca-mobile-client",
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "MCA Mobile Client",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            RedirectUris = { new Uri("nativeapp://callback"), new Uri($"{appBaseUrl}/oauth/demo/callback") },
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
        });
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
}
