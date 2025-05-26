using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Extensions.Extensions;
using System.Security.Claims;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.API.Models;
using MinimalCleanArch.Sample.Domain.Entities;

namespace MinimalCleanArch.Sample.API.Endpoints;

/// <summary>
/// User management endpoints
/// </summary>
public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var userApi = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(); // Require authentication for all user endpoints

        // Get current user profile
        userApi.MapGet("/profile", GetCurrentUserProfile)
            .WithName("GetCurrentUserProfile")
            .WithOpenApi(op => new Microsoft.OpenApi.Models.OpenApiOperation(op)
            {
                Summary = "Gets the current user's profile",
                Description = "Returns the profile information for the currently authenticated user"
            });

        // Update current user profile
        userApi.MapPut("/profile", UpdateCurrentUserProfile)
            .WithName("UpdateCurrentUserProfile")
            .WithValidation<UpdateUserProfileRequest>()
            .WithErrorHandling()
            .WithOpenApi();

        // Get user's todos (example of user-specific data)
        userApi.MapGet("/todos", GetCurrentUserTodos)
            .WithName("GetCurrentUserTodos")
            .WithOpenApi();

        // Admin-only endpoints using roles
        var adminApi = app.MapGroup("/api/admin/users")
            .WithTags("Admin - Users")
            .RequireAuthorization("Admin"); // Require Admin role

        adminApi.MapGet("/", GetAllUsers)
            .WithName("GetAllUsers")
            .WithOpenApi();

        adminApi.MapDelete("/{userId}", DeleteUser)
            .WithName("DeleteUser")
            .WithErrorHandling()
            .WithOpenApi();

        return app;
    }



    private static async Task<IResult> GetCurrentUserProfile(
        ClaimsPrincipal user,
        UserManager<User> userManager)
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

    private static async Task<IResult> UpdateCurrentUserProfile(
        UpdateUserProfileRequest request,
        ClaimsPrincipal user,
        UserManager<User> userManager)
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
            return Results.BadRequest(result.Errors);
        }

        return Results.Ok(new { Message = "Profile updated successfully" });
    }

    private static async Task<IResult> GetCurrentUserTodos(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext)
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

    private static async Task<IResult> GetAllUsers(
        UserManager<User> userManager,
        int page = 1,
        int pageSize = 20)
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

    private static async Task<IResult> DeleteUser(
        string userId,
        UserManager<User> userManager,
        ClaimsPrincipal currentUser)
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
            return Results.BadRequest(result.Errors);
        }

        return Results.NoContent();
    }
}
