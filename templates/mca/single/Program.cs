#if (UseSerilog)
using Serilog;
#endif
using MCA.Application.Services;
using MCA.Domain.Interfaces;
using MCA.Infrastructure.Data;
using MCA.Infrastructure.Repositories;
using MCA.Infrastructure.Services;
using MCA.Endpoints;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Repositories;
using MinimalCleanArch.Repositories;
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

#if (UseSerilog)
// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());
#endif

// Add services to the container

#if (UseSqlite)
// Database - SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=MCA.db";
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
#if (UseSqlServer)
// Database - SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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
#endif
#if (UsePostgres)
// Database - PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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
#endif

// Register DbContext for generic repository
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Repositories
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<ITodoService, TodoService>();
#if (UseAudit)
builder.Services.AddHttpContextAccessor();
#endif

#if (UseSecurity)
// Security - encryption, security headers
builder.Services.AddDataProtectionEncryptionForDevelopment(builder.Environment.ApplicationName ?? "MCA");
#endif

#if (UseCaching)
// Caching
builder.Services.AddMinimalCleanArchCaching();
#endif

#if (UseMessaging)
// Messaging - Wolverine domain events
#if (UseSqlServer)
builder.AddMinimalCleanArchMessagingWithSqlServer(connectionString, options =>
{
    options.ServiceName = "MCA";
});
#elif (UsePostgres)
// Note: PostgreSQL messaging requires WolverineFx.Postgresql package
builder.AddMinimalCleanArchMessaging(options =>
{
    options.ServiceName = "MCA";
});
#else
builder.AddMinimalCleanArchMessaging(options =>
{
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
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MCA"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
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
