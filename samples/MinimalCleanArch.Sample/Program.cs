using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Extensions.Middlewares;
using MinimalCleanArch.Sample.API.Endpoints;
using MinimalCleanArch.Sample.API.Validators;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Security.Configuration;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();

// Ensure logging is configured properly
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Todo API", Version = "v1" });
    options.EnableAnnotations(); // This method is part of Swashbuckle.AspNetCore.Annotations
});


// Add MinimalCleanArch services
builder.Services.AddMinimalCleanArch<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add encryption
var encryptionOptions = new EncryptionOptions
{
    Key = builder.Configuration["Encryption:Key"] ?? "your-strong-encryption-key-at-least-32-characters"
};
builder.Services.AddEncryption(encryptionOptions);

// Add validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateTodoRequestValidator>();

// Add Minimal API extensions
builder.Services.AddMinimalCleanArchExtensions();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Create and migrate database in development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseHttpsRedirection();

// Map API endpoints
app.MapTodoEndpoints();

app.Run();
