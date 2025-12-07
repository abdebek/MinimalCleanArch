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
using MinimalCleanArch.Extensions.Middlewares;
using MinimalCleanArch.Extensions.RateLimiting;
using MinimalCleanArch.Extensions.Telemetry;
using MinimalCleanArch.Extensions.Versioning;
using MinimalCleanArch.Sample.API.Endpoints;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.Infrastructure.Seeders;
using MinimalCleanArch.Sample.Infrastructure.Services;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Extensions;
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

    // Add Serilog with structured logging
    builder.AddSerilogLogging();

    // Add OpenTelemetry observability (tracing and metrics)
    var enableTelemetry = builder.Configuration.GetValue<bool>("Features:Telemetry", true);
    if (enableTelemetry)
    {
        builder.AddMinimalCleanArchTelemetry(options =>
        {
            options.ServiceName = "MinimalCleanArch.Sample";
            options.EnableConsoleExporter = builder.Environment.IsDevelopment();
            options.EnableOtlpExporter = !builder.Environment.IsDevelopment();
        });

        Log.Information("OpenTelemetry observability enabled");
    }

    // Add services to the container
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add API versioning
    builder.Services.AddMinimalCleanArchApiVersioning();

    // Add rate limiting
    builder.Services.AddMinimalCleanArchRateLimiting(config =>
    {
        config.GlobalPermitLimit = 1000;
        config.FixedPermitLimit = 100;
    });

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

    // Add MinimalCleanArch services with Entity Framework
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=minimalcleanarch.db";

    builder.Services.AddMinimalCleanArch<ApplicationDbContext>((sp, options) =>
    {
        options.UseSqlite(connectionString);

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

    // Add validation services
    builder.Services.AddValidatorsFromAssemblyContaining<Todo>();

    // Add caching services (in-memory by default)
    builder.Services.AddMinimalCleanArchCaching(options =>
    {
        options.KeyPrefix = "mca"; // Optional prefix for all cache keys
        options.DefaultExpiration = TimeSpan.FromMinutes(15);
    });

    // Add MinimalCleanArch extensions (includes correlation ID accessor)
    builder.Services.AddMinimalCleanArchExtensions();

    // Add database seeding
    builder.Services.AddDatabaseSeeding()
        .AddSeeder<DatabaseMigrationSeeder>()
        .AddSeeder<RoleSeeder>()
        .AddSeeder<UserSeeder>();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    // Order matters: correlation ID → security headers → error handling → request logging

    // Add correlation ID first
    app.UseCorrelationId();

    // Add security headers
    app.UseSecurityHeaders(SecurityHeadersOptions.ForApi());

    // Add global error handling
    app.UseGlobalErrorHandling();

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    // Add rate limiting
    app.UseMinimalCleanArchRateLimiting();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map health check endpoints
    app.MapMinimalCleanArchHealthChecks();

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

// Make Program class accessible for tests
public partial class Program { }
