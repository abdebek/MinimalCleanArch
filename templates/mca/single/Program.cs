#if (UseSerilog)
using Serilog;
#endif
using MCA.Application.Services;
using MCA.Domain.Interfaces;
using MCA.Infrastructure.Data;
using MCA.Infrastructure.Repositories;
using MCA.Infrastructure.Services;
using MCA.Application.Commands;
using MCA.Application.Handlers;
using MCA.Endpoints;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Repositories;
using MinimalCleanArch.Repositories;
#if (UseValidation)
using FluentValidation;
using MCA.Application.Validation;
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
#if (UseOpenTelemetry)
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System;
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

#if (UseSerilog)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MCA application");
#endif

var builder = WebApplication.CreateBuilder(args);
var dbName = builder.Configuration["DbName"] ?? builder.Environment.ApplicationName ?? "MCA";
var connectionString = BuildConnectionString(dbName);

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

// Services
builder.Services.AddScoped<ITodoService, TodoService>();
#if (UseAudit)
builder.Services.AddHttpContextAccessor();
#endif
#if (UseValidation)
// Validators (used by endpoints and Wolverine handlers)
builder.Services.AddValidatorsFromAssemblyContaining<CreateTodoCommandValidator>();
#endif

#if (UseSecurity)
// Security - encryption, security headers
builder.Services.AddDataProtectionEncryptionForDevelopment(builder.Environment.ApplicationName ?? "MCA");
#endif

#if (UseSecurity)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});
#endif

#if (UseCaching)
// Caching
builder.Services.AddMemoryCache();
builder.Services.AddMinimalCleanArchCaching();
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
// OpenTelemetry
var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:Endpoint");
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MCA"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporterIfConfigured(otlpEndpoint))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());
#endif

// API Explorer for OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#if (UseSerilog)
app.UseSerilogRequestLogging();
#endif


app.UseHttpsRedirection();

#if (UseSecurity)
app.UseCors();
#endif

#if (UseHealthChecks)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
#endif

// Map endpoints
app.MapTodoEndpoints();

// Ensure database is created (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

#if (UseSerilog)
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
#endif

// Expose Program for integration testing
public partial class Program;

#if (UseOpenTelemetry)
static class OpenTelemetryExtensions
{
    public static TracerProviderBuilder AddOtlpExporterIfConfigured(this TracerProviderBuilder builder, string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
        }

        return builder;
    }
}
#endif
