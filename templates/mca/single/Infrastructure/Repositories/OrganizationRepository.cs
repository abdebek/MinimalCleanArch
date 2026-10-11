#if (UseAuth && UseMultiTenant)
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MCA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MCA.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _db;

    public OrganizationRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Organization?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        _db.Organizations.FirstOrDefaultAsync(cancellationToken);

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default) =>
        await _db.Organizations.AddAsync(organization, cancellationToken);

    public async Task AddRoleAsync(OrganizationRole role, CancellationToken cancellationToken = default) =>
        await _db.OrganizationRoles.AddAsync(role, cancellationToken);

    public async Task<IReadOnlyList<OrganizationRole>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await _db.OrganizationRoles.OrderBy(r => r.Name).ToListAsync(cancellationToken);

    public Task<bool> RoleExistsAsync(string name, CancellationToken cancellationToken = default) =>
        _db.OrganizationRoles.AnyAsync(r => r.Name == name, cancellationToken);

    public async Task AddMembershipAsync(OrganizationMembership membership, CancellationToken cancellationToken = default) =>
        await _db.OrganizationMemberships.AddAsync(membership, cancellationToken);

    public Task<OrganizationMembership?> GetMembershipAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _db.OrganizationMemberships.FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<OrganizationMembership>> GetMembersAsync(CancellationToken cancellationToken = default) =>
        await _db.OrganizationMemberships.ToListAsync(cancellationToken);

    public async Task AddInvitationAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default) =>
        await _db.OrganizationInvitations.AddAsync(invitation, cancellationToken);

    public Task<OrganizationInvitation?> GetInvitationByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.OrganizationInvitations.FirstOrDefaultAsync(i => i.Code == code, cancellationToken);
}
#endif
