using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Extensions;
using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Extensions.Middlewares;
using MinimalCleanArch.Sample.API.Endpoints;
using MinimalCleanArch.Sample.API.Validators;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "MinimalCleanArch Sample API", Version = "v1" });
    options.EnableAnnotations();
});

// Add MinimalCleanArch services with Entity Framework
builder.Services.AddMinimalCleanArch<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add encryption services
var encryptionKey = builder.Configuration["Encryption:Key"];
if (string.IsNullOrWhiteSpace(encryptionKey))
{
    // Generate a strong key for development if none provided
    encryptionKey = EncryptionOptions.GenerateStrongKey(64);

    //TODO: log
    //app.Logger.LogWarning("No encryption key configured. Generated a temporary key for development. " +
    //                     "Set Encryption:Key in configuration for production.");
}

var encryptionOptions = new EncryptionOptions
{
    Key = encryptionKey,
    ValidateKeyStrength = !builder.Environment.IsDevelopment(), // Skip validation in development
    EnableOperationLogging = builder.Environment.IsDevelopment()
};

// Validate encryption options
var validationResult = encryptionOptions.Validate();
if (validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
{
    throw new InvalidOperationException($"Invalid encryption configuration: {validationResult.ErrorMessage}");
}

builder.Services.AddEncryption(encryptionOptions);

// Add validation services
builder.Services.AddValidatorsFromAssemblyContaining<CreateTodoRequestValidator>();

// Add MinimalCleanArch extensions
builder.Services.AddMinimalCleanArchExtensions();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MinimalCleanArch Sample API v1");
        c.RoutePrefix = "swagger";
    });

    // Ensure database is created in development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.EnsureCreated();
        app.Logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to initialize database");
        throw;
    }
}

// Add global error handling
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

// Map API endpoints
app.MapTodoEndpoints();

// Add a health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

// Make the implicit Program class public so it can be referenced by tests
public partial class Program { }