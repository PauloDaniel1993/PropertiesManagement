using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Identity;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Identity.Security;
using Alsappan.Application.Identity.Sessions;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;

namespace Alsappan.Application.Tests.Identity;

public sealed class AdministratorServiceTests
{
  [Fact]
  public async Task AdministratorLifecycleWritesSecurityAuditAndTimelineOutboxEvents()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeIdentityRepository();
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var sessions = new RecordingSessionInvalidator();
    var service = CreateService(
      organizationId,
      repository,
      audit,
      outbox,
      sessions,
      [
        PermissionCodes.Write(PermissionModules.Administrators),
        PermissionCodes.Manage(PermissionModules.Administrators),
        PermissionCodes.Archive(PermissionModules.Administrators)
      ]);

    var create = await service.CreateAsync(
      new AdministratorCreateRequestDto(
        "admin.created@example.com",
        "Ana Admin",
        [RoleCodes.OrganizationAdmin]));
    Assert.True(create.Succeeded);

    var update = await service.UpdateAsync(
      create.Value!.Id,
      new AdministratorUpdateRequestDto(
        "admin.updated@example.com",
        "Ana Atualizada",
        [RoleCodes.OrganizationManager]));
    Assert.True(update.Succeeded);

    var deactivate = await service.DeactivateAsync(create.Value.Id);
    Assert.True(deactivate.Succeeded);

    var reactivate = await service.ReactivateAsync(create.Value.Id);
    Assert.True(reactivate.Succeeded);

    var archive = await service.ArchiveAsync(create.Value.Id);
    Assert.True(archive.Succeeded);

    Assert.Equal(
      [
        "administrators.invited",
        "administrators.role.changed",
        "administrators.deactivated",
        "administrators.reactivated",
        "administrators.archived"
      ],
      audit.Entries.Select(entry => entry.Action));
    Assert.All(audit.Entries, entry => Assert.Equal(AuditEntryCategory.Security, entry.Category));

    Assert.Equal(audit.Entries.Select(entry => entry.Action), outbox.Envelopes.Select(envelope => envelope.EventName));
    Assert.All(outbox.Envelopes, envelope =>
    {
      Assert.Equal("administrators", envelope.ModuleName);
      Assert.Equal("identityUser", envelope.Subject.EntityType);
      Assert.True(envelope.Consumers.HasFlag(ModuleEventConsumer.Timeline));
      Assert.True(envelope.Consumers.HasFlag(ModuleEventConsumer.Notifications));
      Assert.False(envelope.Consumers.HasFlag(ModuleEventConsumer.Audit));
    });
    Assert.Equal(4, sessions.RevokedUserIds.Count);
  }

  private static AdministratorService CreateService(
    OrganizationId organizationId,
    FakeIdentityRepository repository,
    RecordingAuditWriter auditWriter,
    RecordingOutboxWriter outboxWriter,
    RecordingSessionInvalidator sessionInvalidator,
    IEnumerable<string> permissions) =>
    new(
      repository,
      new FakePasswordHashService(),
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      sessionInvalidator,
      auditWriter,
      outboxWriter,
      TimeProvider.System);

  private sealed class FixedPermissionService : IPermissionService
  {
    private readonly HashSet<string> permissions;

    public FixedPermissionService(IEnumerable<string> permissions)
    {
      this.permissions = new HashSet<string>(
        permissions.Select(PermissionCodes.Normalize),
        StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      string permissionCode,
      CancellationToken cancellationToken = default) =>
      AuthorizeAsync(new PermissionRequirement(permissionCode), cancellationToken);

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      PermissionRequirement requirement,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var organizationId = OrganizationId.New();
      var granted = permissions.Contains(PermissionCodes.Wildcard) ||
        permissions.Contains(PermissionCodes.Normalize(requirement.PermissionCode));

      return ValueTask.FromResult(
        granted
          ? PermissionEvaluationResult.Granted(requirement, organizationId)
          : PermissionEvaluationResult.Denied(
            requirement,
            PermissionEvaluationFailure.PermissionDenied,
            organizationId));
    }

    public ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult<IReadOnlySet<string>>(permissions);
    }
  }

  private sealed class FixedActiveOrganizationContextResolver : IActiveOrganizationContextResolver
  {
    private readonly ActiveOrganizationContext context;

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId)
    {
      var membership = new OrganizationMembership(organizationId, permissionCodes: [PermissionCodes.Wildcard]);
      var user = new AuthenticatedUser(UserId.New(), "owner@alsappan.local", "Paulo", [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
    }
  }

  private sealed class FakeIdentityRepository : IIdentityRepository
  {
    private readonly List<IdentityUser> users = [];
    private readonly List<IdentityMembership> memberships = [];
    private readonly List<UserInvitation> invitations = [];
    private readonly List<ResidentAccountLink> residentAccountLinks = [];

    public Task AddUserAsync(
      IdentityUser user,
      IEnumerable<IdentityMembership> memberships,
      UserInvitation? invitation = null,
      ResidentAccountLink? residentAccountLink = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      users.Add(user);
      this.memberships.AddRange(memberships);
      if (invitation is not null)
      {
        invitations.Add(invitation);
      }

      if (residentAccountLink is not null)
      {
        residentAccountLinks.Add(residentAccountLink);
      }

      return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(
      string email,
      UserAccountType accountType,
      UserId? excludingUserId = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var normalizedEmail = IdentityCode.NormalizeEmail(email);
      return Task.FromResult(users.Any(user =>
        user.AccountType == accountType &&
        user.NormalizedEmail == normalizedEmail &&
        user.Id != excludingUserId));
    }

    public Task<IdentityOrganization?> FindOrganizationAsync(
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IdentityOrganization?>(null);
    }

    public Task<IdentityMembership?> FindMembershipAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(memberships.FirstOrDefault(membership =>
        membership.UserId == userId &&
        membership.OrganizationId == organizationId));
    }

    public Task<ResidentAccountLink?> FindResidentAccountLinkAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(residentAccountLinks.FirstOrDefault(link =>
        link.UserId == userId &&
        link.OrganizationId == organizationId &&
        link.IsActive));
    }

    public Task<IdentityRole?> FindRoleByCodeAsync(
      string roleCode,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IdentityRole?>(IdentityRole.Create(
        EntityId.New(),
        organizationId,
        roleCode,
        roleCode,
        DateTimeOffset.UtcNow));
    }

    public Task<ResidentAccountLink?> FindResidentAccountLinkByResidentAsync(
      EntityId residentId,
      OrganizationId organizationId,
      bool includeInactive = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(residentAccountLinks.FirstOrDefault(link =>
        link.ResidentId == residentId &&
        link.OrganizationId == organizationId &&
        (includeInactive || link.IsActive)));
    }

    public Task<IdentityUser?> FindUserByEmailAsync(
      string email,
      UserAccountType accountType,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var normalizedEmail = IdentityCode.NormalizeEmail(email);
      return Task.FromResult(users.FirstOrDefault(user =>
        user.AccountType == accountType &&
        user.NormalizedEmail == normalizedEmail));
    }

    public Task<IdentityUser?> FindUserByIdAsync(
      UserId userId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(users.FirstOrDefault(user => user.Id == userId));
    }

    public Task<AdministratorDetailDto?> GetAdministratorAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var user = users.FirstOrDefault(candidate => candidate.Id == userId);
      var membership = memberships.FirstOrDefault(candidate =>
        candidate.UserId == userId &&
        candidate.OrganizationId == organizationId);
      if (user is null || membership is null)
      {
        return Task.FromResult<AdministratorDetailDto?>(null);
      }

      return Task.FromResult<AdministratorDetailDto?>(
        new AdministratorDetailDto(
          user.Id.Value,
          user.Email,
          user.DisplayName ?? user.Email,
          user.Status.ToString(),
          membership.RoleCodes,
          membership.PermissionCodes,
          user.CreatedAt,
          user.UpdatedAt,
          user.LastLoginAt,
          null));
    }

    public Task<IReadOnlyList<IdentityMembership>> ListMembershipsAsync(
      UserId userId,
      bool includeInactive = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<IdentityMembership>>(
        memberships.Where(membership => membership.UserId == userId).ToArray());
    }

    public Task<IReadOnlyList<IdentityOrganization>> ListOrganizationsAsync(
      IEnumerable<OrganizationId> organizationIds,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<IdentityOrganization>>([]);
    }

    public Task<PagedResultDto<AdministratorListItemDto>> ListAdministratorsAsync(
      AdministratorListRequestDto filter,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(new PagedResultDto<AdministratorListItemDto>(
        [],
        filter.Page,
        filter.PageSize,
        0));
    }

    public Task AddMembershipAsync(IdentityMembership membership, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      memberships.Add(membership);
      return Task.CompletedTask;
    }

    public Task AddUserInvitationAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      invitations.Add(invitation);
      return Task.CompletedTask;
    }

    public Task AddResidentAccountLinkAsync(ResidentAccountLink link, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      residentAccountLinks.Add(link);
      return Task.CompletedTask;
    }

    public Task UpdateMembershipAsync(IdentityMembership membership, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }

    public Task UpdateUserAsync(IdentityUser user, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }

    public Task UpdateResidentAccountLinkAsync(
      ResidentAccountLink link,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }
  }

  private sealed class FakePasswordHashService : IPasswordHashService
  {
    public string HashPassword(IdentityUser user, string password) => $"hashed:{password}";

    public PasswordVerificationResult VerifyPassword(
      IdentityUser user,
      string password,
      string passwordHash) =>
      PasswordVerificationResult.Success;
  }

  private sealed class RecordingAuditWriter : IAuditWriter
  {
    public List<AuditEntryDraft> Entries { get; } = [];

    public Task WriteAsync(AuditEntryDraft entry, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Entries.Add(entry);
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingOutboxWriter : IModuleEventOutboxWriter
  {
    public List<ModuleEventEnvelope> Envelopes { get; } = [];

    public Task EnqueueAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Envelopes.Add(envelope);
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingSessionInvalidator : IIdentitySessionInvalidator
  {
    public List<UserId> RevokedUserIds { get; } = [];

    public Task RevokeUserSessionsAsync(
      UserId userId,
      DateTimeOffset revokedAt,
      string reason,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      RevokedUserIds.Add(userId);
      return Task.CompletedTask;
    }
  }
}
