#if (UseSerilog)
using Serilog;
#endif
using Scalar.AspNetCore;
using MCA.Domain.Interfaces;
using MCA.Infrastructure.Data;
using MCA.Infrastructure.Repositories;
using MCA.Application.Commands;
using MCA.Application.Handlers;
using MCA.Endpoints;
#if (UseAuth)
using MCA.Infrastructure.Services;
#endif
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Repositories;
using MinimalCleanArch.Repositories;
#if (UseValidation)
using MCA.Application.Validation;
#endif
#if (UseMcaApiBootstrap)
using MinimalCleanArch.Extensions.Extensions;
#endif
#if (UseHealthChecks)
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
#endif
#if (UseSecurity)
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Extensions;
#endif
#if (UseCaching)
using MinimalCleanArch.Extensions.Caching;
#endif
#if (UseMessaging)
using MinimalCleanArch.Messaging.Extensions;
#endif
#if (UseAudit)
using MinimalCleanArch.Audit.Extensions;
#endif
#if (UseStorage)
using MinimalCleanArch.Storage;
#endif
#if (UseOpenTelemetry)
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System;
#endif
#if (UseAuth)
using MCA.Application.Interfaces;
using MCA.Infrastructure.Configuration;
#endif
#if (UseMessaging)
using Wolverine;
#if (UseValidation)
using Wolverine.FluentValidation;
#endif
#if (UseDurableMessaging && UseSqlServer)
using Wolverine.SqlServer;
#endif
#if (UseDurableMessaging && UsePostgres)
using Wolverine.Postgresql;
#endif
#endif

var builder = WebApplication.CreateBuilder(args);
#if (UseAspire)
builder.AddServiceDefaults();
#endif
var dbName = builder.Configuration["DbName"] ?? builder.Environment.ApplicationName ?? "MCA";
// Aspire injects ConnectionStrings:appdb for Postgres/SQL Server resources; otherwise DefaultConnection / fallbacks
var connectionString =
#if (UseAspire)
    builder.Configuration.GetConnectionString("appdb")
    ?? BuildConnectionString(dbName);
#else
    BuildConnectionString(dbName);
#endif

string BuildConnectionString(string databaseName)
{
    var configured = builder.Configuration.GetConnectionString("DefaultConnection");

#if (UsePostgres)
    var fallback = $"Host=localhost;Database={databaseName};Username=postgres;Password=postgres";
#elif (UseSqlServer)
    var fallback = $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
#else
    var fallback = $"Data Source={databaseName}.db";
#endif

    if (string.IsNullOrWhiteSpace(configured))
    {
        return fallback;
    }

    return configured.Replace("{dbName}", databaseName);
}

#if (UseSerilog)
// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());
#endif

// Add services to the container

#if (UsePostgres)
// Database - PostgreSQL
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
#if (UseAuth)
    options.UseOpenIddict<Guid>();
#endif
#if (UseAudit)
    options.UseAuditInterceptor(sp);
#endif
#if (UseMessaging)
    options.UseDomainEventPublishing(sp);
#endif
});
#elif (UseSqlServer)
// Database - SQL Server
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
#if (UseAuth)
    options.UseOpenIddict<Guid>();
#endif
#if (UseAudit)
    options.UseAuditInterceptor(sp);
#endif
#if (UseMessaging)
    options.UseDomainEventPublishing(sp);
#endif
});
#else
// Database - SQLite (default)
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlite(connectionString);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
#if (UseAuth)
    options.UseOpenIddict<Guid>();
#endif
#if (UseAudit)
    options.UseAuditInterceptor(sp);
#endif
#if (UseMessaging)
    options.UseDomainEventPublishing(sp);
#endif
});
#endif

// Repositories
builder.Services.AddScoped<ITodoRepository>(sp => new TodoRepository(sp.GetRequiredService<AppDbContext>()));
builder.Services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(sp.GetRequiredService<AppDbContext>()));

// Application handlers (use cases)
builder.Services.AddScoped<TodoCommandHandler>();
#if (UseAudit)
builder.Services.AddHttpContextAccessor();
#endif

#if (UseMcaApiBootstrap)
// Preferred MCA API bootstrap: problem details, correlation, validators, rate limiting
builder.Services.AddMinimalCleanArchApi(options =>
{
#if (UseValidation)
    options.AddValidatorsFromAssemblyContaining<CreateTodoCommandValidator>();
#endif
#if (UseRateLimiting)
    options.EnableRateLimiting = true;
    options.ConfigureRateLimiting = config =>
        builder.Configuration.GetSection("RateLimiting").Bind(config);
#endif
});
#endif

#if (UseSecurity)
// Security - encryption (dev Data Protection vs configured key outside Development)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtectionEncryptionForDevelopment(builder.Environment.ApplicationName ?? "MCA");
}
else
{
    var encryptionKey = builder.Configuration["Encryption:Key"];
    if (string.IsNullOrWhiteSpace(encryptionKey))
    {
        throw new InvalidOperationException(
            "Encryption:Key must be configured outside Development (user-secrets, environment variable, or vault). " +
            "Do not commit production keys to appsettings.json.");
    }

    builder.Services.AddEncryption(new EncryptionOptions
    {
        Key = encryptionKey,
        ValidateKeyStrength = true,
        EnableOperationLogging = false
    });
}
#endif

#if (UseStorage)
// Blob storage (Azure Blob Storage / Azurite). Configure BlobStorage:* in appsettings or env.
builder.Services.AddAzureBlobStorage(builder.Configuration);
#endif

#if (UseSecurity)
// CORS: configured origins only; Development may fall back to AllowAnyOrigin when the list is empty
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            // Fail closed: no origins allowed until Cors:AllowedOrigins is configured
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});
#endif

#if (UseAuth)
// Authentication - OpenIddict + ASP.NET Core Identity
builder.Services.AddAuthServices(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<ChangePasswordHandler>();
builder.Services.AddScoped<ConfirmEmailHandler>();
builder.Services.AddScoped<ForgotPasswordHandler>();
builder.Services.AddScoped<ResetPasswordHandler>();
builder.Services.AddScoped<AuthLoginHandler>();
builder.Services.AddScoped<AuthLogoutHandler>();
builder.Services.AddScoped<ExternalAuthSignInHandler>();
builder.Services.AddHttpClient();
#endif

#if (UseCaching)
// Caching
#if (UseAspire)
var redisConnection = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.AddRedisDistributedCache("redis");
    builder.Services.AddMinimalCleanArchDistributedCaching();
}
else
{
    builder.Services.AddMemoryCache();
    builder.Services.AddMinimalCleanArchCaching();
}
#else
builder.Services.AddMemoryCache();
builder.Services.AddMinimalCleanArchCaching();
#endif
#endif

#if (UseMessaging)
// Messaging - Wolverine domain events
#if (UseSqlServer)
builder.AddMinimalCleanArchMessagingWithSqlServer(connectionString, options =>
{
    options.IncludeAssembly(typeof(TodoCommandHandler).Assembly);
    options.ServiceName = "MCA";
});

#elif (UsePostgres)
builder.AddMinimalCleanArchMessagingWithPostgres(connectionString, options =>
{
    options.IncludeAssembly(typeof(TodoCommandHandler).Assembly);
    options.ServiceName = "MCA";
});
#else
builder.AddMinimalCleanArchMessaging(options =>
{
    options.IncludeAssembly(typeof(TodoCommandHandler).Assembly);
    options.ServiceName = "MCA";
});
#endif
#endif

#if (UseAudit)
// Audit logging
builder.Services.AddAuditLogging();
builder.Services.AddAuditLogService<AppDbContext>();
#endif

#if (UseHealthChecks)
// Health checks
builder.Services.AddHealthChecks()
#if (UseSqlite)
    .AddSqlite(connectionString, name: "database");
#endif
#if (UseSqlServer)
    .AddSqlServer(connectionString, name: "database");
#endif
#if (UsePostgres)
    .AddNpgSql(connectionString, name: "database");
#endif
#endif

#if (UseOpenTelemetry)
// OpenTelemetry (skip console wiring when Aspire OTLP is active)
var aspireOtlp = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
if (!aspireOtlp)
{
    var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:Endpoint");
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("MCA"))
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            }
        })
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());
}
#endif

// OpenAPI document generation
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
#if (UseAuth)
        var webClientSecret = app.Configuration["OpenIddict:Clients:Web:Secret"];
        var webClientId = app.Configuration["OpenIddict:Clients:Web:ClientId"] ?? "mca-web-client";
        options.AddPasswordFlow("oauth2", flow =>
        {
            flow.TokenUrl = "/connect/token";
            flow.ClientId = webClientId;
            flow.ClientSecret = webClientSecret;
            flow.SelectedScopes = new[]
            {
                "openid",
                "profile",
                "email",
                "offline_access",
                "mca.api"
            };
        });
        options.AddPreferredSecuritySchemes(new[] { "oauth2" });
        options.WithPersistentAuthentication();
#endif
    });
}

#if (UseMcaApiBootstrap)
// Preferred MCA pipeline: correlation ID → security headers → error handling → optional rate limiting
app.UseMinimalCleanArchApiDefaults(pipeline =>
{
#if (UseRateLimiting)
    pipeline.UseRateLimiting = true;
#endif
#if (UseSecurity)
    pipeline.UseApiSecurityHeaders = true;
#endif
});
#endif

#if (UseSerilog)
app.UseSerilogRequestLogging();
#endif

app.UseHttpsRedirection();

#if (UseSecurity)
app.UseCors();
#endif

#if (UseAuth)
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
#endif

#if (UseHealthChecks)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
#endif
#if (UseAspire)
app.MapDefaultEndpoints();
#endif

// Map endpoints
app.MapTodoEndpoints();
#if (UseStorage)
app.MapStorageEndpoints();
#endif
#if (UseAuth)
app.MapAuthEndpoints(app.Environment.IsDevelopment());
app.MapOpenIddictEndpoints(app.Environment.IsDevelopment());
app.MapExternalAuthEndpoints();
app.MapOAuthEndpoints(app.Environment.IsDevelopment());
#endif

// Database schema (EnsureCreated for SQLite demos; Migrate/fallback for SQL Server/PostgreSQL)
var dbLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
await DatabaseInitializer.InitializeAsync(
    app.Services,
    app.Configuration,
    app.Environment,
    dbLogger);
#if (UseAuth)
// OpenIddict clients + optional bootstrap admin (controlled by Seed:* settings)
await app.Services.SeedOpenIddictApplicationsAsync(builder.Configuration);
#endif

app.Run();

// Expose Program for integration testing
public partial class Program;
