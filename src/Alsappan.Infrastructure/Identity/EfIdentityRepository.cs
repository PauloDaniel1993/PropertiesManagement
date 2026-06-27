using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Identity;

public sealed class EfIdentityRepository : IIdentityRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfIdentityRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public Task<IdentityUser?> FindUserByIdAsync(
    UserId userId,
    CancellationToken cancellationToken = default) =>
    dbContext.Set<IdentityUser>()
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

  public Task<IdentityUser?> FindUserByEmailAsync(
    string email,
    UserAccountType accountType,
    CancellationToken cancellationToken = default)
  {
    var normalizedEmail = IdentityCode.NormalizeEmail(email);
    return dbContext.Set<IdentityUser>()
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(
        user => user.NormalizedEmail == normalizedEmail && user.AccountType == accountType,
        cancellationToken);
  }

  public Task<bool> EmailExistsAsync(
    string email,
    UserAccountType accountType,
    UserId? excludingUserId = null,
    CancellationToken cancellationToken = default)
  {
    var normalizedEmail = IdentityCode.NormalizeEmail(email);
    return dbContext.Set<IdentityUser>()
      .IgnoreQueryFilters()
      .AnyAsync(
        user =>
          user.NormalizedEmail == normalizedEmail &&
          user.AccountType == accountType &&
          (!excludingUserId.HasValue || user.Id != excludingUserId.Value),
        cancellationToken);
  }

  public async Task<IReadOnlyList<IdentityMembership>> ListMembershipsAsync(
    UserId userId,
    bool includeInactive = false,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Set<IdentityMembership>()
      .IgnoreQueryFilters()
      .Where(membership => membership.UserId == userId);

    if (!includeInactive)
    {
      query = query.Where(membership =>
        membership.DeletedAt == null &&
        membership.Status == IdentityMembershipStatus.Active);
    }

    return await query
      .OrderBy(membership => membership.OrganizationId.Value)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public Task<IdentityMembership?> FindMembershipAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default) =>
    dbContext.Set<IdentityMembership>()
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(
        membership =>
          membership.UserId == userId &&
          membership.OrganizationId == organizationId &&
          membership.DeletedAt == null,
        cancellationToken);

  public async Task<IReadOnlyList<IdentityOrganization>> ListOrganizationsAsync(
    IEnumerable<OrganizationId> organizationIds,
    CancellationToken cancellationToken = default)
  {
    var ids = organizationIds.Distinct().ToArray();
    if (ids.Length == 0)
    {
      return [];
    }

    return await dbContext.Set<IdentityOrganization>()
      .IgnoreQueryFilters()
      .Where(organization => ids.Contains(organization.Id) && organization.DeletedAt == null)
      .OrderBy(organization => organization.DisplayName)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public Task<IdentityOrganization?> FindOrganizationAsync(
    OrganizationId organizationId,
    CancellationToken cancellationToken = default) =>
    dbContext.Set<IdentityOrganization>()
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(
        organization => organization.Id == organizationId && organization.DeletedAt == null,
        cancellationToken);

  public Task<IdentityRole?> FindRoleByCodeAsync(
    string roleCode,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var normalizedRoleCode = RoleCodes.Normalize(roleCode);
    return dbContext.Set<IdentityRole>()
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(
        role =>
          role.OrganizationId == organizationId &&
          role.Code == normalizedRoleCode &&
          role.DeletedAt == null,
        cancellationToken);
  }

  public Task<ResidentAccountLink?> FindResidentAccountLinkAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default) =>
    dbContext.Set<ResidentAccountLink>()
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(
        link =>
          link.UserId == userId &&
          link.OrganizationId == organizationId &&
          link.IsActive &&
          link.DeletedAt == null,
        cancellationToken);

  public Task<ResidentAccountLink?> FindResidentAccountLinkByResidentAsync(
    EntityId residentId,
    OrganizationId organizationId,
    bool includeInactive = false,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Set<ResidentAccountLink>()
      .IgnoreQueryFilters()
      .Where(link =>
        link.ResidentId == residentId &&
        link.OrganizationId == organizationId &&
        link.DeletedAt == null);

    if (!includeInactive)
    {
      query = query.Where(link => link.IsActive);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task AddUserAsync(
    IdentityUser user,
    IEnumerable<IdentityMembership> memberships,
    UserInvitation? invitation = null,
    ResidentAccountLink? residentAccountLink = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(memberships);

    dbContext.Set<IdentityUser>().Add(user);
    dbContext.Set<IdentityMembership>().AddRange(memberships);

    if (invitation is not null)
    {
      dbContext.Set<UserInvitation>().Add(invitation);
    }

    if (residentAccountLink is not null)
    {
      dbContext.Set<ResidentAccountLink>().Add(residentAccountLink);
    }

    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task AddMembershipAsync(
    IdentityMembership membership,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(membership);

    dbContext.Set<IdentityMembership>().Add(membership);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task AddUserInvitationAsync(
    UserInvitation invitation,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(invitation);

    dbContext.Set<UserInvitation>().Add(invitation);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task AddResidentAccountLinkAsync(
    ResidentAccountLink link,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(link);

    dbContext.Set<ResidentAccountLink>().Add(link);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateUserAsync(
    IdentityUser user,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(user);

    dbContext.Set<IdentityUser>().Update(user);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateMembershipAsync(
    IdentityMembership membership,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(membership);

    dbContext.Set<IdentityMembership>().Update(membership);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateResidentAccountLinkAsync(
    ResidentAccountLink link,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(link);

    dbContext.Set<ResidentAccountLink>().Update(link);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task<PagedResultDto<AdministratorListItemDto>> ListAdministratorsAsync(
    AdministratorListRequestDto filter,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(filter);

    var listFilter = new ListFilterDto(
      filter.Page,
      filter.PageSize,
      filter.Search,
      includeArchived: filter.IncludeArchived);
    var query = BuildAdministratorQuery(organizationId, filter);
    var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var rows = await query
      .OrderBy(row => row.User.DisplayName ?? row.User.Email)
      .ThenBy(row => row.User.Email)
      .Skip(listFilter.Offset)
      .Take(listFilter.PageSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var items = rows
      .Select(row => new AdministratorListItemDto(
        row.User.Id.Value,
        row.User.Email,
        row.User.DisplayName ?? row.User.Email,
        ToStatusCode(row.User.Status),
        row.Membership.RoleCodes,
        row.User.CreatedAt,
        row.User.LastLoginAt))
      .ToArray();

    return new PagedResultDto<AdministratorListItemDto>(
      items,
      listFilter.Page,
      listFilter.PageSize,
      total);
  }

  public async Task<AdministratorDetailDto?> GetAdministratorAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var row = await BuildAdministratorQuery(
        organizationId,
        new AdministratorListRequestDto(IncludeArchived: true))
      .FirstOrDefaultAsync(row => row.User.Id == userId, cancellationToken)
      .ConfigureAwait(false);
    if (row is null)
    {
      return null;
    }

    return new AdministratorDetailDto(
      row.User.Id.Value,
      row.User.Email,
      row.User.DisplayName ?? row.User.Email,
      ToStatusCode(row.User.Status),
      row.Membership.RoleCodes,
      row.Membership.PermissionCodes,
      row.User.CreatedAt,
      row.User.UpdatedAt,
      row.User.LastLoginAt,
      row.User.ConcurrencyToken.Value);
  }

  private IQueryable<AdministratorQueryRow> BuildAdministratorQuery(
    OrganizationId organizationId,
    AdministratorListRequestDto filter)
  {
    var users = dbContext.Set<IdentityUser>()
      .IgnoreQueryFilters()
      .Where(user => user.AccountType == UserAccountType.Admin);

    var memberships = dbContext.Set<IdentityMembership>()
      .IgnoreQueryFilters()
      .Where(membership => membership.OrganizationId == organizationId);

    if (!filter.IncludeArchived)
    {
      users = users.Where(user => user.DeletedAt == null && user.Status != UserStatus.Archived);
      memberships = memberships.Where(membership =>
        membership.DeletedAt == null &&
        membership.Status != IdentityMembershipStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(filter.Search))
    {
      var search = filter.Search.Trim();
      var normalizedSearch = IdentityCode.NormalizeEmail(search);
      var displayNamePattern = $"%{search}%";
      users = users.Where(user =>
        user.NormalizedEmail.Contains(normalizedSearch) ||
        (user.DisplayName != null && EF.Functions.ILike(user.DisplayName, displayNamePattern)));
    }

    if (TryParseUserStatus(filter.Status, out var status))
    {
      users = users.Where(user => user.Status == status);
    }

    if (!string.IsNullOrWhiteSpace(filter.Role))
    {
      var role = IdentityCode.NormalizeCode(filter.Role);
      memberships = memberships.Where(membership => membership.RoleCodes.Contains(role));
    }

    return users.Join(
      memberships,
      user => user.Id,
      membership => membership.UserId,
      (user, membership) => new AdministratorQueryRow(user, membership));
  }

  private static bool TryParseUserStatus(string? value, out UserStatus status)
  {
    status = default;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    return Enum.TryParse(value.Trim(), ignoreCase: true, out status);
  }

  private static string ToStatusCode(UserStatus status) =>
    IdentityCode.NormalizeCode(status.ToString());

  private sealed record AdministratorQueryRow(
    IdentityUser User,
    IdentityMembership Membership);
}
