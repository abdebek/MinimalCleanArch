using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Extensions;
using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Extensions.Middlewares;
using MinimalCleanArch.Sample.API.Endpoints;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user tracking
builder.Services.AddHttpContextAccessor();

// Add MinimalCleanArch services with Entity Framework
builder.Services.AddMinimalCleanArch<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the DbContext as base class for repositories
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// Add Identity services
builder.Services.AddIdentityApiEndpoints<User>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add MinimalCleanArch repositories (using the ApplicationDbContext)
builder.Services.AddMinimalCleanArchRepositories();


// Add encryption services
var encryptionKey = builder.Configuration["Encryption:Key"];
if (string.IsNullOrWhiteSpace(encryptionKey))
{
    // Generate a strong key for development
    encryptionKey = EncryptionOptions.GenerateStrongKey(64);
    builder.Services.AddLogging();

    //TODO: log
    //var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
    //logger?.LogWarning("No encryption key configured. Generated a temporary key for development.");
}

var encryptionOptions = new EncryptionOptions
{
    Key = encryptionKey,
    ValidateKeyStrength = !builder.Environment.IsDevelopment(),
    EnableOperationLogging = builder.Environment.IsDevelopment()
};

builder.Services.AddEncryption(encryptionOptions);

// Add validation services
builder.Services.AddValidatorsFromAssemblyContaining<Todo>();

// Add MinimalCleanArch extensions
builder.Services.AddMinimalCleanArchExtensions();


builder.Services.AddAuthorizationBuilder();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // 🔥 ONLY create if database doesn't exist
    if (!await dbContext.Database.CanConnectAsync())
    {
        await dbContext.Database.EnsureCreatedAsync();
        await SeedDataAsync(scope.ServiceProvider);
    }
    else
    {
        // Database exists, just seed if needed
        await SeedDataAsync(scope.ServiceProvider);
    }
}

// Add global error handling
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map Identity endpoints
app.MapIdentityApi<User>();

// Map your application endpoints
app.MapTodoEndpoints();
app.MapUserEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

// Seed initial data
static async Task SeedDataAsync(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
    
    // Create admin user
    var adminEmail = "admin@example.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "System Administrator"
        };
        
        await userManager.CreateAsync(adminUser, "Admin123!");
    }
}

// Make Program class accessible for tests
public partial class Program { }