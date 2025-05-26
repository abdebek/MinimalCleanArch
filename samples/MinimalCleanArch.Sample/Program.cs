using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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


// Use Identity API endpoints (simpler, includes MapIdentityApi)
builder.Services.AddIdentityApiEndpoints<User>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add role management manually for AddIdentityApiEndpoints
builder.Services.AddScoped<RoleManager<IdentityRole>>();
builder.Services.AddScoped<IRoleStore<IdentityRole>, RoleStore<IdentityRole, ApplicationDbContext>>();


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

// Add authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
    .AddPolicy("User", policy => policy.RequireRole("User"));

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
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation($"Created role: {role}");
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
                FullName = "System Administrator"
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Admin user created and assigned to Admin role");
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
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Added Admin role to existing admin user");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding data");
    }
}

// Make Program class accessible for tests
public partial class Program { }