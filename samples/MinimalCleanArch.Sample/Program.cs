using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Extensions;
using MinimalCleanArch.Extensions.Configuration;
using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Extensions.HealthChecks;
using MinimalCleanArch.Extensions.Hosting;
using MinimalCleanArch.Extensions.Logging;
using MinimalCleanArch.Extensions.Middlewares;
using MinimalCleanArch.Extensions.RateLimiting;
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

    // Add MinimalCleanArch services with Entity Framework
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=minimalcleanarch.db";

    builder.Services.AddMinimalCleanArch<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));

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
