#if (UseAuth && UseMultiTenant)
using MCA.Domain.Entities;

namespace MCA.Domain.Interfaces;

public interface IOrganizationRepository
{
    Task<Organization?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
    Task AddRoleAsync(OrganizationRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationRole>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(string name, CancellationToken cancellationToken = default);
    Task AddMembershipAsync(OrganizationMembership membership, CancellationToken cancellationToken = default);
    Task<OrganizationMembership?> GetMembershipAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationMembership>> GetMembersAsync(CancellationToken cancellationToken = default);
    Task AddInvitationAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default);
    Task<OrganizationInvitation?> GetInvitationByCodeAsync(string code, CancellationToken cancellationToken = default);
}
#endif
