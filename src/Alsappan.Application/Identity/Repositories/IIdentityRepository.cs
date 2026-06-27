using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;

namespace Alsappan.Application.Identity.Repositories;

public interface IIdentityRepository
{
  Task<IdentityUser?> FindUserByIdAsync(UserId userId, CancellationToken cancellationToken = default);

  Task<IdentityUser?> FindUserByEmailAsync(
    string email,
    UserAccountType accountType,
    CancellationToken cancellationToken = default);

  Task<bool> EmailExistsAsync(
    string email,
    UserAccountType accountType,
    UserId? excludingUserId = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<IdentityMembership>> ListMembershipsAsync(
    UserId userId,
    bool includeInactive = false,
    CancellationToken cancellationToken = default);

  Task<IdentityMembership?> FindMembershipAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<IdentityOrganization>> ListOrganizationsAsync(
    IEnumerable<OrganizationId> organizationIds,
    CancellationToken cancellationToken = default);

  Task<IdentityOrganization?> FindOrganizationAsync(
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<ResidentAccountLink?> FindResidentAccountLinkAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task AddUserAsync(
    IdentityUser user,
    IEnumerable<IdentityMembership> memberships,
    UserInvitation? invitation = null,
    ResidentAccountLink? residentAccountLink = null,
    CancellationToken cancellationToken = default);

  Task UpdateUserAsync(IdentityUser user, CancellationToken cancellationToken = default);

  Task UpdateMembershipAsync(IdentityMembership membership, CancellationToken cancellationToken = default);

  Task<PagedResultDto<AdministratorListItemDto>> ListAdministratorsAsync(
    AdministratorListRequestDto filter,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<AdministratorDetailDto?> GetAdministratorAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);
}
