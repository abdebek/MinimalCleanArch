#if (UseAuth && UseMultiTenant)
using MCA.Application.DTOs;

namespace MCA.Application.Commands;

public record CreateOrganizationCommand(string Name);

public record GetCurrentOrganizationQuery;

public record GetOrganizationRolesQuery;

public record GetOrganizationMembersQuery;

public record CreateOrganizationInvitationCommand(string Role, string? Email = null, int ExpiresInDays = 7);

public record JoinOrganizationCommand(string Code);

public record OrganizationListResult<T>(IReadOnlyList<T> Items);
#endif
