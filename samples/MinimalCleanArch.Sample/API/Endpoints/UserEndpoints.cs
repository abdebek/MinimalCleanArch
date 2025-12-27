using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Extensions.Extensions;
using System.Security.Claims;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.API.Models;
using MinimalCleanArch.Sample.Domain.Entities;

namespace MinimalCleanArch.Sample.API.Endpoints;

/// <summary>
/// User management endpoints with proper role-based authorization
/// </summary>
public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var userApi = app.MapGroup("/api/users")
            .WithTags("Users");

        // Public endpoints (no authentication required)
        userApi.MapPost("/register", RegisterUser)
            .WithName("RegisterUser")
            .WithValidation<RegisterUserRequest>()
            .WithErrorHandling()
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account");

        userApi.MapPost("/login", LoginUser)
            .WithName("LoginUser")
            .WithValidation<LoginUserRequest>()
            .WithErrorHandling();

        // Authenticated user endpoints
        var authUserApi = userApi.MapGroup("")
            .RequireAuthorization(); // Require authentication for these endpoints

        // Get current user profile
        authUserApi.MapGet("/profile", GetCurrentUserProfile)
            .WithName("GetCurrentUserProfile")
            .WithSummary("Gets the current user's profile")
            .WithDescription("Returns the profile information for the currently authenticated user");

        // Update current user profile
        authUserApi.MapPut("/profile", UpdateCurrentUserProfile)
            .WithName("UpdateCurrentUserProfile")
            .WithValidation<UpdateUserProfileRequest>()
            .WithErrorHandling();

        // Get user's todos (example of user-specific data)
        authUserApi.MapGet("/todos", GetCurrentUserTodos)
            .WithName("GetCurrentUserTodos");

        // Admin-only endpoints using roles
        var adminApi = app.MapGroup("/api/admin/users")
            .WithTags("Admin - Users")
            .RequireAuthorization("Admin"); // Require Admin role

        adminApi.MapGet("/", GetAllUsers)
            .WithName("GetAllUsers");

        adminApi.MapDelete("/{userId}", DeleteUser)
            .WithName("DeleteUser")
            .WithErrorHandling();

        adminApi.MapPost("/{userId}/roles/{roleName}", AssignRole)
            .WithName("AssignRole")
            .WithErrorHandling();

        adminApi.MapDelete("/{userId}/roles/{roleName}", RemoveRole)
            .WithName("RemoveRole")
            .WithErrorHandling();

        return app;
    }

    private static async Task<IResult> RegisterUser(
        RegisterUserRequest request,
        UserManager<User> userManager)
    {
        try
        {
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { 
                    Message = "Registration failed", 
                    Errors = result.Errors.Select(e => e.Description).ToArray() 
                });
            }

            // Assign default User role
            await userManager.AddToRoleAsync(user, "User");

            return Results.Ok(new { 
                Message = "User registered successfully", 
                UserId = user.Id 
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Registration Error",
                detail: $"An error occurred during registration: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> LoginUser(
        LoginUserRequest request,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Results.BadRequest(new { Message = "Invalid email or password" });
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return Results.BadRequest(new { Message = "Account is locked out" });
                }
                if (result.IsNotAllowed)
                {
                    return Results.BadRequest(new { Message = "Account is not allowed to sign in" });
                }
                return Results.BadRequest(new { Message = "Invalid email or password" });
            }

            // Get user roles
            var userRoles = await userManager.GetRolesAsync(user);

            return Results.Ok(new
            {
                Message = "Login successful",
                User = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FullName,
                    Roles = userRoles
                }
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Login Error",
                detail: $"An error occurred during login: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetCurrentUserProfile(
        ClaimsPrincipal user,
        UserManager<User> userManager)
    {
        try
        {
            var currentUser = await userManager.GetUserAsync(user);
            if (currentUser == null)
            {
                return Results.NotFound("User not found");
            }

            // Get user roles
            var userRoles = await userManager.GetRolesAsync(currentUser);

            var profile = new UserProfileResponse
            {
                Id = currentUser.Id,
                UserName = currentUser.UserName!,
                Email = currentUser.Email!,
                FullName = currentUser.FullName,
                DateOfBirth = currentUser.DateOfBirth,
                EmailConfirmed = currentUser.EmailConfirmed,
                PhoneNumber = currentUser.PhoneNumber,
                Roles = userRoles.ToList(),
                CreatedAt = currentUser.CreatedAt,
                LastModifiedAt = currentUser.LastModifiedAt
            };

            return Results.Ok(profile);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Profile Error",
                detail: $"An error occurred while retrieving profile: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateCurrentUserProfile(
        UpdateUserProfileRequest request,
        ClaimsPrincipal user,
        UserManager<User> userManager)
    {
        try
        {
            var currentUser = await userManager.GetUserAsync(user);
            if (currentUser == null)
            {
                return Results.NotFound("User not found");
            }

            // Update allowed fields
            currentUser.FullName = request.FullName;
            currentUser.DateOfBirth = request.DateOfBirth;
            currentUser.PhoneNumber = request.PhoneNumber;
            currentUser.PersonalNotes = request.PersonalNotes; // This will be encrypted

            var result = await userManager.UpdateAsync(currentUser);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { 
                    Message = "Update failed", 
                    Errors = result.Errors.Select(e => e.Description).ToArray() 
                });
            }

            return Results.Ok(new { Message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Update Error",
                detail: $"An error occurred while updating profile: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetCurrentUserTodos(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            // Get todos created by the current user
            var todos = await dbContext.Todos
                .Where(t => t.CreatedBy == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TodoResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    IsCompleted = t.IsCompleted,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    LastModifiedAt = t.LastModifiedAt
                })
                .ToListAsync();

            return Results.Ok(todos);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Todos Error",
                detail: $"An error occurred while retrieving todos: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetAllUsers(
        UserManager<User> userManager,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var users = await userManager.Users
                .Where(u => !u.IsDeleted) // Use soft delete filter
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSummaryResponse
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Email = u.Email!,
                    FullName = u.FullName,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            var totalCount = await userManager.Users.Where(u => !u.IsDeleted).CountAsync();

            return Results.Ok(new
            {
                Users = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Users Error",
                detail: $"An error occurred while retrieving users: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteUser(
        string userId,
        UserManager<User> userManager,
        ClaimsPrincipal currentUser)
    {
        try
        {
            // Prevent self-deletion
            var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == currentUserId)
            {
                return Results.BadRequest("Cannot delete your own account");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Results.NotFound("User not found");
            }

            // Soft delete the user
            user.IsDeleted = true;
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return Results.BadRequest(new { 
                    Message = "Delete failed", 
                    Errors = result.Errors.Select(e => e.Description).ToArray() 
                });
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Delete Error",
                detail: $"An error occurred while deleting user: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> AssignRole(
        string userId,
        string roleName,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Results.NotFound("User not found");
            }

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                return Results.BadRequest($"Role '{roleName}' does not exist");
            }

            if (await userManager.IsInRoleAsync(user, roleName))
            {
                return Results.BadRequest($"User already has role '{roleName}'");
            }

            var result = await userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { 
                    Message = "Role assignment failed", 
                    Errors = result.Errors.Select(e => e.Description).ToArray() 
                });
            }

            return Results.Ok(new { Message = $"Role '{roleName}' assigned successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Role Assignment Error",
                detail: $"An error occurred while assigning role: {ex.Message}",
                statusCode: 500);
        }
    }

    private static async Task<IResult> RemoveRole(
        string userId,
        string roleName,
        UserManager<User> userManager)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Results.NotFound("User not found");
            }

            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                return Results.BadRequest($"User does not have role '{roleName}'");
            }

            var result = await userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { 
                    Message = "Role removal failed", 
                    Errors = result.Errors.Select(e => e.Description).ToArray() 
                });
            }

            return Results.Ok(new { Message = $"Role '{roleName}' removed successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Role Removal Error",
                detail: $"An error occurred while removing role: {ex.Message}",
                statusCode: 500);
        }
    }
}
