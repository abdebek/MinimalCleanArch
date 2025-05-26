using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalCleanArch.Sample.API.Models;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Encryption;
using FluentAssertions;

namespace MinimalCleanArch.IntegrationTests.API;

/// <summary>
/// Simplified integration tests for User endpoints without complex authentication
/// </summary>
public class UserEndpointsSimpleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public UserEndpointsSimpleIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _databaseName = $"test_users_simple_{Guid.NewGuid()}.db";
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var contextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ApplicationDbContext));
                
                if (contextDescriptor != null)
                {
                    services.Remove(contextDescriptor);
                }

                // Remove existing encryption services
                services.RemoveAll<EncryptionOptions>();
                services.RemoveAll<IEncryptionService>();

                // Add simplified test encryption service
                var encryptionOptions = new EncryptionOptions
                {
                    Key = EncryptionOptions.GenerateStrongKey(64),
                    ValidateKeyStrength = false,
                    EnableOperationLogging = false,
                    AllowEnvironmentVariables = false
                };
                
                services.AddSingleton(encryptionOptions);
                services.AddSingleton<IEncryptionService, AesEncryptionService>();
                
                // Add test database
                services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                {
                    options.UseSqlite($"Data Source={_databaseName}");
                    options.EnableServiceProviderCaching(false);
                    options.EnableSensitiveDataLogging();
                }, ServiceLifetime.Scoped);
            });
        });
        
        _client = _factory.CreateClient();
        
        // Ensure database is created and seeded
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        SeedTestData(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    #region Basic Registration Tests
      
    //TODO: fix
    //[Fact]
    //public async Task RegisterUser_WithValidData_ShouldReturnSuccess()
    //{
    //    // Arrange
    //    var uniqueEmail = $"newuser_{Guid.NewGuid():N}@test.com";
    //    var registerRequest = new RegisterUserRequest
    //    {
    //        Email = uniqueEmail,
    //        Password = "StrongPassword123!",
    //        ConfirmPassword = "StrongPassword123!",
    //        FullName = "Test User",
    //        DateOfBirth = DateTime.Now.AddYears(-25)
    //    };

    //    // Act
    //    var response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);

    //    // Assert
    //    response.StatusCode.Should().Be(HttpStatusCode.OK);
        
    //    var content = await response.Content.ReadAsStringAsync();
    //    content.Should().Contain("User registered successfully");
    //    content.Should().Contain("UserId");

    //    // Verify user was created in database
    //    using var scope = _factory.Services.CreateScope();
    //    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    //    var user = await userManager.FindByEmailAsync(registerRequest.Email);
        
    //    user.Should().NotBeNull();
    //    user!.Email.Should().Be(registerRequest.Email);
    //    user.FullName.Should().Be(registerRequest.FullName);
    //}

    [Fact]
    public async Task RegisterUser_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterUserRequest
        {
            Email = "invalid-email-format",
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!",
            FullName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    //TODO: fix
    //[Fact] 
    //public async Task RegisterUser_WithWeakPassword_ShouldReturnBadRequest()
    //{
    //    // Arrange
    //    var registerRequest = new RegisterUserRequest
    //    {
    //        Email = $"test_{Guid.NewGuid():N}@example.com",
    //        Password = "weak",
    //        ConfirmPassword = "weak",
    //        FullName = "Test User"
    //    };

    //    // Act
    //    var response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);

    //    // Assert
    //    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
    //    var content = await response.Content.ReadAsStringAsync();
    //    content.Should().Contain("Registration failed");
    //}

    [Fact]
    public async Task RegisterUser_WithMismatchedPasswords_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterUserRequest
        {
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "DifferentPassword123!",
            FullName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Basic Login Tests

    [Fact]
    public async Task LoginUser_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange - First register a user
        var email = $"logintest_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";
        
        var registerRequest = new RegisterUserRequest
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FullName = "Login Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/users/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginRequest = new LoginUserRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Login successful");
        content.Should().Contain(email);
    }

    [Fact]
    public async Task LoginUser_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new LoginUserRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginUser_WithInvalidPassword_ShouldReturnBadRequest()
    {
        // Arrange - Use the seeded test user
        var loginRequest = new LoginUserRequest
        {
            Email = "testuser@example.com",
            Password = "WrongPassword!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    #endregion

    #region Authorization Tests (Simple)

    [Fact]
    public async Task GetCurrentUserProfile_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCurrentUserProfile_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateRequest = new UpdateUserProfileRequest
        {
            FullName = "Updated Name"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/users/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUserTodos_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUsers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteUser_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync("/api/admin/users/some-user-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region User Management Tests
      
    //TODO: fix
    //[Fact]
    //public async Task RegisterMultipleUsers_ShouldCreateUsersWithUniqueIds()
    //{
    //    // Arrange
    //    var users = new List<RegisterUserRequest>();
    //    for (int i = 0; i < 3; i++)
    //    {
    //        users.Add(new RegisterUserRequest
    //        {
    //            Email = $"multiuser_{i}_{Guid.NewGuid():N}@test.com",
    //            Password = "Password123!",
    //            ConfirmPassword = "Password123!",
    //            FullName = $"Multi User {i}"
    //        });
    //    }

    //    // Act & Assert
    //    var userIds = new List<string>();
    //    foreach (var userRequest in users)
    //    {
    //        var response = await _client.PostAsJsonAsync("/api/users/register", userRequest);
    //        response.StatusCode.Should().Be(HttpStatusCode.OK);
            
    //        var content = await response.Content.ReadAsStringAsync();
    //        content.Should().Contain("UserId");
            
    //        // Extract and verify unique user IDs (simplified check)
    //        using var scope = _factory.Services.CreateScope();
    //        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    //        var user = await userManager.FindByEmailAsync(userRequest.Email);
    //        user.Should().NotBeNull();
    //        userIds.Add(user!.Id);
    //    }

    //    // All user IDs should be unique
    //    userIds.Should().OnlyHaveUniqueItems();
    //    userIds.Should().HaveCount(3);
    //}

    [Fact]
    public async Task RegisterUser_ShouldAssignDefaultUserRole()
    {
        // Arrange
        var email = $"roletest_{Guid.NewGuid():N}@test.com";
        var registerRequest = new RegisterUserRequest
        {
            Email = email,
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FullName = "Role Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user has User role
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(email);
        
        user.Should().NotBeNull();
        var roles = await userManager.GetRolesAsync(user!);
        roles.Should().Contain("User");
    }

    [Fact]
    public async Task User_ShouldHaveAuditFields()
    {
        // Arrange
        var email = $"audittest_{Guid.NewGuid():N}@test.com";
        var registerRequest = new RegisterUserRequest
        {
            Email = email,
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FullName = "Audit Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit fields are set
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(email);
        
        user.Should().NotBeNull();
        user!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        user.CreatedBy.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Data Validation Tests

    [Fact]
    public async Task RegisterUser_WithEmptyFullName_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterUserRequest
        {
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FullName = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginUser_WithEmptyCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new LoginUserRequest
        {
            Email = "",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Database Integration Tests

    [Fact]
    public async Task UserRegistration_ShouldPersistToDatabase()
    {
        // Arrange
        var email = $"dbtest_{Guid.NewGuid():N}@test.com";
        var fullName = "Database Test User";
        var registerRequest = new RegisterUserRequest
        {
            Email = email,
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FullName = fullName,
            DateOfBirth = DateTime.Now.AddYears(-30)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify data persistence
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.FullName.Should().Be(fullName);
        user.EmailConfirmed.Should().BeFalse(); // Default value
        user.IsDeleted.Should().BeFalse(); // Soft delete default
        user.DateOfBirth.Should().NotBeNull();
        user.DateOfBirth!.Value.Date.Should().Be(registerRequest.DateOfBirth!.Value.Date);
    }

    #endregion

    #region Helper Methods

    private async Task SeedTestData(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            // Create roles
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create test user
            var testEmail = "testuser@example.com";
            if (await userManager.FindByEmailAsync(testEmail) == null)
            {
                var testUser = new User
                {
                    UserName = testEmail,
                    Email = testEmail,
                    EmailConfirmed = true,
                    FullName = "Test User",
                    PersonalNotes = "Test user for integration tests"
                };

                var result = await userManager.CreateAsync(testUser, "TestPassword123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                }
            }
        }
        catch (Exception)
        {
            // Ignore seeding errors in tests
        }
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
        
        // Clean up test database file
        try
        {
            if (File.Exists(_databaseName))
            {
                File.Delete(_databaseName);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}