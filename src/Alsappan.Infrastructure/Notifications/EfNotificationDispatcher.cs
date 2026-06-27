using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Notifications;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Notifications;

public sealed class EfNotificationDispatcher : INotificationDispatcher
{
  private readonly AlsappanDbContext _dbContext;
  private readonly IRolePermissionCatalog _rolePermissionCatalog;
  private readonly TimeProvider _clock;

  public EfNotificationDispatcher(
    AlsappanDbContext dbContext,
    IRolePermissionCatalog rolePermissionCatalog,
    TimeProvider? clock = null)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _rolePermissionCatalog = rolePermissionCatalog ?? throw new ArgumentNullException(nameof(rolePermissionCatalog));
    _clock = clock ?? TimeProvider.System;
  }

  public async Task DispatchAsync(
    ModuleEventEnvelope envelope,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    if (!envelope.Consumers.HasFlag(ModuleEventConsumer.Notifications))
    {
      return;
    }

    var recipients = await ListEligibleRecipientIdsAsync(envelope, cancellationToken).ConfigureAwait(false);
    if (recipients.Count == 0)
    {
      return;
    }

    var createdAt = _clock.GetUtcNow();
    _dbContext.NotificationRecords.AddRange(
      recipients.Select(recipient =>
        NotificationRecord.FromEnvelope(envelope, createdAt, recipient)));
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private async Task<IReadOnlyList<UserId>> ListEligibleRecipientIdsAsync(
    ModuleEventEnvelope envelope,
    CancellationToken cancellationToken)
  {
    var sourcePermission = NotificationCatalog.GetReadPermissionForCategory(envelope.ModuleName);
    if (sourcePermission is null)
    {
      return [];
    }

    var candidates = await _dbContext.IdentityMemberships
      .IgnoreQueryFilters()
      .Join(
        _dbContext.IdentityUsers.IgnoreQueryFilters(),
        membership => membership.UserId,
        user => user.Id,
        (membership, user) => new
        {
          Membership = membership,
          User = user
        })
      .Where(row =>
        row.Membership.OrganizationId == envelope.OrganizationId &&
        row.Membership.DeletedAt == null &&
        row.Membership.Status == IdentityMembershipStatus.Active &&
        row.User.DeletedAt == null &&
        row.User.Status == UserStatus.Active)
      .Select(row => new RecipientCandidate(
        row.Membership.UserId,
        row.Membership.RoleCodes,
        row.Membership.PermissionCodes))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return candidates
      .Where(candidate => CanReceive(candidate, sourcePermission))
      .Select(candidate => candidate.UserId)
      .Distinct()
      .ToArray();
  }

  private bool CanReceive(RecipientCandidate candidate, string sourcePermission)
  {
    var permissions = new HashSet<string>(candidate.PermissionCodes, StringComparer.OrdinalIgnoreCase);
    permissions.UnionWith(_rolePermissionCatalog.GetPermissionsForRoles(candidate.RoleCodes));

    return HasPermission(permissions, PermissionCodes.Read(PermissionModules.Notifications)) &&
      HasPermission(permissions, sourcePermission);
  }

  private static bool HasPermission(HashSet<string> permissions, string permissionCode) =>
    permissions.Contains(PermissionCodes.Wildcard) ||
    permissions.Contains(PermissionCodes.Normalize(permissionCode));

  private sealed record RecipientCandidate(
    UserId UserId,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> PermissionCodes);
}
