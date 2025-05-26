namespace MinimalCleanArch.Sample.API.Models;

public class UserProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}

public class UserSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserProfileRequest
{
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PersonalNotes { get; set; } // This will be encrypted
}