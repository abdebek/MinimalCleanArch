using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Audit.Extensions;
using MinimalCleanArch.DataAccess.Extensions;
using MinimalCleanArch.Extensions.Caching;
using MinimalCleanArch.Extensions.Configuration;
using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Extensions.HealthChecks;
using MinimalCleanArch.Extensions.Hosting;
using MinimalCleanArch.Extensions.Logging;
using MinimalCleanArch.Extensions.Telemetry;
using MinimalCleanArch.Extensions.Versioning;
using MinimalCleanArch.Messaging.Extensions;
using MinimalCleanArch.Sample.API.Endpoints;
using MinimalCleanArch.Sample.API.Validators;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.Infrastructure.Seeders;
using MinimalCleanArch.Sample.Infrastructure.Services;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Extensions;
using Scalar.AspNetCore;
using Serilog;

// Configure Serilog early for startup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MinimalCleanArch Sample application");

    var builder = WebApplication.CreateBuilder(args);

    // Aspire service defaults (OTLP → dashboard when OTEL_EXPORTER_OTLP_ENDPOINT is set)
    builder.AddServiceDefaults();

    // Add Serilog with structured logging
    builder.AddSerilogLogging();

    // Prefer Aspire OTLP; only enable MCA telemetry when not under Aspire OTLP
    var aspireOtlp = !string.IsNullOrWhiteSpace(
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    var enableTelemetry = builder.Configuration.GetValue<bool>("Features:Telemetry", true);
    if (enableTelemetry && !aspireOtlp)
    {
        builder.AddMinimalCleanArchTelemetry(options =>
        {
            options.ServiceName = "MinimalCleanArch.Sample";
            options.EnableConsoleExporter = builder.Environment.IsDevelopment();
            options.EnableOtlpExporter = !builder.Environment.IsDevelopment();
        });

        Log.Information("OpenTelemetry observability enabled (MCA exporters)");
    }
    else if (aspireOtlp)
    {
        Log.Information("OpenTelemetry via Aspire OTLP (ServiceDefaults); MCA console exporters skipped");
    }

    // Add services to the container
    builder.Services.AddOpenApi();

    // Preferred MCA API bootstrap (problem details, correlation, validators, rate limiting)
    builder.Services.AddMinimalCleanArchApi(options =>
    {
        // Scan the sample assembly (API validators live next to endpoints/models)
        options.AddValidatorsFromAssemblyContaining<CreateTodoRequestValidator>();
        options.EnableRateLimiting = true;
        options.ConfigureRateLimiting = config =>
        {
            config.GlobalPermitLimit = 1000;
            config.FixedPermitLimit = 100;
        };
    });

    // Add API versioning
    builder.Services.AddMinimalCleanArchApiVersioning();

    // Add health checks
    builder.Services.AddMinimalCleanArchHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database", tags: ["readiness", "database"]);

    // Add HTTP context accessor for user tracking
    builder.Services.AddHttpContextAccessor();

    // Add encryption services FIRST (before DbContext)
    var encryptionKey = builder.Configuration["Encryption:Key"];
    if (string.IsNullOrWhiteSpace(encryptionKey))
    {
        encryptionKey = EncryptionOptions.GenerateStrongKey(64);
        Log.Warning("No encryption key configured. Generated a temporary key for development. " +
                   "Set 'Encryption:Key' in configuration for production.");
    }

    var encryptionOptions = new EncryptionOptions
    {
        Key = encryptionKey,
        ValidateKeyStrength = !builder.Environment.IsDevelopment(),
        EnableOperationLogging = builder.Environment.IsDevelopment()
    };

    builder.Services.AddEncryption(encryptionOptions);

    // OPT-IN: Add audit logging with change history tracking
    // This is optional - remove to disable audit logging
    var enableAuditLogging = builder.Configuration.GetValue<bool>("Features:AuditLogging", true);
    if (enableAuditLogging)
    {
        builder.Services.AddAuditLogging(options =>
        {
            options.CaptureOldValues = true;
            options.CaptureNewValues = true;
            options.TrackChangedProperties = true;
            options.CaptureClientIp = true;
            // Exclude sensitive properties from audit logs
            options.ExcludeProperty("PasswordHash");
            options.ExcludeProperty("SecurityStamp");
        });

        // Add audit log query service
        builder.Services.AddAuditLogService<ApplicationDbContext>();

        Log.Information("Audit logging enabled with change history tracking");
    }

    // Connection strings: Aspire injects "mca" (Postgres) and "redis"; local default is SQLite
    var connectionString = builder.Configuration.GetConnectionString("mca")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=minimalcleanarch.db";
    var usePostgres = LooksLikePostgres(connectionString);

    builder.Services.AddMinimalCleanArch<ApplicationDbContext>((sp, options) =>
    {
        if (usePostgres)
        {
            options.UseNpgsql(connectionString);
            Log.Information("Using PostgreSQL (connection name mca or Postgres-shaped DefaultConnection)");
        }
        else
        {
            options.UseSqlite(connectionString);
            Log.Information("Using SQLite");
        }

        // Add audit interceptor if enabled
        if (enableAuditLogging)
        {
            options.UseAuditInterceptor(sp);
        }
    });

    // Add Identity API endpoints with roles support
    builder.Services.AddIdentityApiEndpoints<User>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;

        // Sign in settings
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

    // Add authorization policies
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
        .AddPolicy("User", policy => policy.RequireRole("User"));

    // Add email services (production only)
    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddValidatedOptions<EmailSettings>(
            builder.Configuration,
            settings => !string.IsNullOrEmpty(settings.SmtpServer),
            "EmailSettings:SmtpServer is required");
        builder.Services.AddScoped<IEmailSender, EmailSender>();
    }

    // Caching: Redis when Aspire injects ConnectionStrings:redis; otherwise in-memory
    var redisConnection = builder.Configuration.GetConnectionString("redis");
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        builder.AddRedisDistributedCache("redis");
        builder.Services.AddMinimalCleanArchDistributedCaching(options =>
        {
            options.KeyPrefix = "mca";
            options.DefaultExpiration = TimeSpan.FromMinutes(15);
        });
        Log.Information("Distributed caching enabled (Redis via Aspire connection name 'redis')");
    }
    else
    {
        builder.Services.AddMinimalCleanArchCaching(options =>
        {
            options.KeyPrefix = "mca";
            options.DefaultExpiration = TimeSpan.FromMinutes(15);
        });
    }

    // OPT-IN: Add messaging with Wolverine for domain events
    // This enables the mediator pattern and event-driven architecture
    var enableMessaging = builder.Configuration.GetValue<bool>("Features:Messaging", true);
    if (enableMessaging)
    {
        // Use in-memory mode for development (SQLite doesn't support Wolverine's outbox)
        // For production with SQL Server, use AddMinimalCleanArchMessagingWithSqlServer()
        builder.AddMinimalCleanArchMessaging(options =>
        {
            options.ServiceName = "MinimalCleanArch.Sample";
        });

        Log.Information("Wolverine messaging enabled for domain events");
    }

    // Add database seeding
    builder.Services.AddDatabaseSeeding()
        .AddSeeder<DatabaseMigrationSeeder>()
        .AddSeeder<RoleSeeder>()
        .AddSeeder<UserSeeder>();

    var app = builder.Build();

    // Preferred MCA pipeline: correlation ID → security headers → error handling → rate limiting
    app.UseMinimalCleanArchApiDefaults(pipeline =>
    {
        pipeline.UseRateLimiting = true;
        pipeline.UseApiSecurityHeaders = true;
    });

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();

    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map health check endpoints (MCA /health* + Aspire /alive + /health/ready when OTLP is set)
    app.MapMinimalCleanArchHealthChecks();
    if (aspireOtlp)
    {
        app.MapDefaultEndpoints();
    }

    // Map Identity API endpoints - provides /register, /login, etc.
    app.MapIdentityApi<User>();

    // Map your application endpoints
    app.MapTodoEndpoints();
    app.MapUserEndpoints();

    // Mark startup complete for health checks
    app.MarkStartupComplete();

    Log.Information("Application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static bool LooksLikePostgres(string connectionString) =>
    connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("User ID=", StringComparison.OrdinalIgnoreCase);

// Make Program class accessible for tests
public partial class Program { }
