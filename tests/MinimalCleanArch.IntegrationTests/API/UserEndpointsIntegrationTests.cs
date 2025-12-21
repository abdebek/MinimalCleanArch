using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.IntegrationTests.Infrastructure;
using MinimalCleanArch.Sample.API.Models;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using FluentAssertions;

namespace MinimalCleanArch.IntegrationTests.API;

/// <summary>
/// Simplified integration tests for User endpoints without complex authentication
/// </summary>
public class UserEndpointsSimpleIntegrationTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private const string DefaultPassword = "StrongPassword123!";

    public UserEndpointsSimpleIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureDatabaseCreatedAsync();
        await SeedTestDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

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

    #region Authenticated User Tests

    [Fact]
    public async Task GetCurrentUserProfile_WithAuthentication_ReturnsProfile()
    {
        var email = $"profile_{Guid.NewGuid():N}@example.com";
        var user = await _factory.CreateUserAsync(email, DefaultPassword, "User");
        using var client = _factory.CreateAuthenticatedClient(user.Id, email, "User");

        var response = await client.GetAsync("/api/users/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(user.Id);
        profile.Email.Should().Be(email);
        profile.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task UpdateCurrentUserProfile_WithAuthentication_UpdatesProfile()
    {
        var email = $"update_{Guid.NewGuid():N}@example.com";
        var user = await _factory.CreateUserAsync(email, DefaultPassword, "User");
        using var client = _factory.CreateAuthenticatedClient(user.Id, email, "User");

        var updateRequest = new UpdateUserProfileRequest
        {
            FullName = "Updated User",
            DateOfBirth = new DateTime(1990, 1, 1),
            PhoneNumber = "5551234567",
            PersonalNotes = "Notes for update"
        };

        var response = await client.PutAsJsonAsync("/api/users/profile", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var updatedUser = await userManager.FindByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FullName.Should().Be(updateRequest.FullName);
        updatedUser.DateOfBirth.Should().Be(updateRequest.DateOfBirth);
        updatedUser.PhoneNumber.Should().Be(updateRequest.PhoneNumber);
    }

    [Fact]
    public async Task GetCurrentUserTodos_WithAuthentication_ReturnsOnlyOwnTodos()
    {
        var token = Guid.NewGuid().ToString("N");
        var userAEmail = $"usera_{token}@example.com";
        var userBEmail = $"userb_{token}@example.com";

        var userA = await _factory.CreateUserAsync(userAEmail, DefaultPassword, "User");
        var userB = await _factory.CreateUserAsync(userBEmail, DefaultPassword, "User");

        using var clientA = _factory.CreateAuthenticatedClient(userA.Id, userAEmail, "User");
        using var clientB = _factory.CreateAuthenticatedClient(userB.Id, userBEmail, "User");

        var todoA = new CreateTodoRequest
        {
            Title = $"Todo A {token}",
            Description = "Owned by A",
            Priority = 1
        };

        var todoB = new CreateTodoRequest
        {
            Title = $"Todo B {token}",
            Description = "Owned by B",
            Priority = 1
        };

        (await clientA.PostAsJsonAsync("/api/todos", todoA)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await clientB.PostAsJsonAsync("/api/todos", todoB)).StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await clientA.GetAsync("/api/users/todos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<List<TodoResponse>>();
        todos.Should().NotBeNull();
        todos!.Should().ContainSingle(t => t.Title == todoA.Title);
        todos.Should().NotContain(t => t.Title == todoB.Title);
    }

    #endregion

    #region Admin User Tests

    [Fact]
    public async Task GetAllUsers_WithAdminRole_ReturnsUsers()
    {
        var adminEmail = $"admin_{Guid.NewGuid():N}@example.com";
        var adminUser = await _factory.CreateUserAsync(adminEmail, DefaultPassword, "Admin");
        var regularEmail = $"user_{Guid.NewGuid():N}@example.com";
        await _factory.CreateUserAsync(regularEmail, DefaultPassword, "User");

        using var client = _factory.CreateAuthenticatedClient(adminUser.Id, adminEmail, "Admin");
        var response = await client.GetAsync("/api/admin/users?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<UserListResponse>();
        payload.Should().NotBeNull();
        payload!.Users.Should().Contain(user => user.Email == regularEmail);
        payload.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllUsers_WithNonAdminRole_ReturnsForbidden()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var user = await _factory.CreateUserAsync(email, DefaultPassword, "User");
        using var client = _factory.CreateAuthenticatedClient(user.Id, email, "User");

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_WithAdminRole_SoftDeletes()
    {
        var adminEmail = $"admin_{Guid.NewGuid():N}@example.com";
        var adminUser = await _factory.CreateUserAsync(adminEmail, DefaultPassword, "Admin");
        var targetEmail = $"delete_{Guid.NewGuid():N}@example.com";
        var targetUser = await _factory.CreateUserAsync(targetEmail, DefaultPassword, "User");

        using var client = _factory.CreateAuthenticatedClient(adminUser.Id, adminEmail, "Admin");
        var response = await client.DeleteAsync($"/api/admin/users/{targetUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deletedUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == targetUser.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_WithAdminRole_ShouldRejectSelfDeletion()
    {
        var adminEmail = $"admin_{Guid.NewGuid():N}@example.com";
        var adminUser = await _factory.CreateUserAsync(adminEmail, DefaultPassword, "Admin");
        using var client = _factory.CreateAuthenticatedClient(adminUser.Id, adminEmail, "Admin");

        var response = await client.DeleteAsync($"/api/admin/users/{adminUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignRole_WithAdminRole_AssignsRole()
    {
        const string roleName = "Manager";
        await EnsureRoleExistsAsync(roleName);

        var adminEmail = $"admin_{Guid.NewGuid():N}@example.com";
        var adminUser = await _factory.CreateUserAsync(adminEmail, DefaultPassword, "Admin");
        var targetEmail = $"role_{Guid.NewGuid():N}@example.com";
        var targetUser = await _factory.CreateUserAsync(targetEmail, DefaultPassword, "User");

        using var client = _factory.CreateAuthenticatedClient(adminUser.Id, adminEmail, "Admin");
        var response = await client.PostAsync($"/api/admin/users/{targetUser.Id}/roles/{roleName}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roles = await userManager.GetRolesAsync(targetUser);
        roles.Should().Contain(roleName);
    }

    [Fact]
    public async Task RemoveRole_WithAdminRole_RemovesRole()
    {
        const string roleName = "Manager";
        await EnsureRoleExistsAsync(roleName);

        var adminEmail = $"admin_{Guid.NewGuid():N}@example.com";
        var adminUser = await _factory.CreateUserAsync(adminEmail, DefaultPassword, "Admin");
        var targetEmail = $"role_{Guid.NewGuid():N}@example.com";
        var targetUser = await _factory.CreateUserAsync(targetEmail, DefaultPassword, roleName);

        using var client = _factory.CreateAuthenticatedClient(adminUser.Id, adminEmail, "Admin");
        var response = await client.DeleteAsync($"/api/admin/users/{targetUser.Id}/roles/{roleName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roles = await userManager.GetRolesAsync(targetUser);
        roles.Should().NotContain(roleName);
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

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

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

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private sealed class UserListResponse
    {
        public List<UserSummaryResponse> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
    }
}
