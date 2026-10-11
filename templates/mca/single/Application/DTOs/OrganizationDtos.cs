#if (UseAuth && UseMultiTenant)
namespace MCA.Application.DTOs;

public record CreateOrganizationRequest(string Name);

public record CreateInvitationRequest(string Role, string? Email = null, int ExpiresInDays = 7);

public record JoinOrganizationRequest(string Code);

public record OrganizationResponse(Guid Id, string Name, string TenantId, string Role);

public record OrganizationRoleResponse(Guid Id, string Name);

public record OrganizationMemberResponse(Guid UserId, string Role);

public record OrganizationInvitationResponse(
    Guid Id,
    string Code,
    string Role,
    string? Email,
    DateTime ExpiresAt);

public record JoinOrganizationResponse(Guid OrganizationId, string TenantId, string Role);
#endif
