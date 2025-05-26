using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Extensions;
using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Extensions.Middlewares;
using MinimalCleanArch.Sample.API.Endpoints;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.Infrastructure.Services;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP context accessor for user tracking
builder.Services.AddHttpContextAccessor();

// Add encryption services FIRST (before DbContext)
var encryptionKey = builder.Configuration["Encryption:Key"];
if (string.IsNullOrWhiteSpace(encryptionKey))
{
    encryptionKey = EncryptionOptions.GenerateStrongKey(64);
}

var encryptionOptions = new EncryptionOptions
{
    Key = encryptionKey,
    ValidateKeyStrength = !builder.Environment.IsDevelopment(),
    EnableOperationLogging = builder.Environment.IsDevelopment()
};

builder.Services.AddEncryption(encryptionOptions);

// Add MinimalCleanArch services with Entity Framework
builder.Services.AddMinimalCleanArch<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity API endpoints with roles support - this is the modern approach
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
.AddRoles<IdentityRole>() // Add roles support
.AddEntityFrameworkStores<ApplicationDbContext>();

// Add authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
    .AddPolicy("User", policy => policy.RequireRole("User"));

// Add email services
if (!builder.Environment.IsDevelopment()) 
{ 
    var emailSettings = new EmailSettings();
    builder.Configuration.GetSection("EmailSettings").Bind(emailSettings);
    builder.Services.AddSingleton(emailSettings);
    builder.Services.AddScoped<IEmailSender, EmailSender>();
}

// Add validation services
builder.Services.AddValidatorsFromAssemblyContaining<Todo>();

// Add MinimalCleanArch extensions
builder.Services.AddMinimalCleanArchExtensions();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Only create if database doesn't exist
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

// Map Identity API endpoints - provides /register, /login, etc.
app.MapIdentityApi<User>();

// Map your application endpoints
app.MapTodoEndpoints();
app.MapUserEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

// Seed initial data with roles
static async Task SeedDataAsync(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Create roles
        var roles = new[] { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {Role}", role);
                }
                else
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
        
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
                FullName = "System Administrator",
                PersonalNotes = "Initial admin user created during setup"
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Admin user created and assigned to Admin role");
                }
                else
                {
                    logger.LogError("Failed to assign Admin role: {Errors}",
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure admin user has Admin role
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Added Admin role to existing admin user");
                }
                else
                {
                    logger.LogError("Failed to add Admin role to existing user: {Errors}",
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        // Create a regular user for testing
        var userEmail = "user@example.com";
        var regularUser = await userManager.FindByEmailAsync(userEmail);
        
        if (regularUser == null)
        {
            regularUser = new User
            {
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true,
                FullName = "Test User",
                PersonalNotes = "Regular user for testing purposes"
            };
            
            var result = await userManager.CreateAsync(regularUser, "User123!");
            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(regularUser, "User");
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Regular user created and assigned to User role");
                }
                else
                {
                    logger.LogError("Failed to assign User role: {Errors}",
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("Failed to create regular user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding data");
        throw;
    }
}

// Make Program class accessible for tests
public partial class Program { }