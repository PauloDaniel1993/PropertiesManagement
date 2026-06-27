using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Identity.Security;
using Alsappan.Application.Identity.Sessions;
using Alsappan.Application.Residents;
using Alsappan.Application.Residents.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Residents;

namespace Alsappan.Application.Tests.Residents;

public sealed class ResidentAccountServiceTests
{
  [Fact]
  public async Task AccountLifecycleInvitesActivatesResetsDeactivatesAndUnlinksResident()
  {
    var fixture = ResidentAccountFixture.Create([PermissionCodes.Write(PermissionModules.Residents)]);
    var residentId = fixture.Residents.Resident.Id.Value;

    var invited = await fixture.Service.InviteAsync(
      residentId,
      new ResidentAccountInviteRequestDto("resident@example.com"));

    Assert.True(invited.Succeeded);
    Assert.NotNull(invited.Value!.InvitationToken);
    Assert.Equal("invited", invited.Value.PortalStatus.Code);
    Assert.False(fixture.Identity.Links.Single().IsActive);
    Assert.Equal(ResidentPortalStatus.Invited, fixture.Residents.Resident.PortalStatus);

    var activated = await fixture.Service.ActivateAsync(
      residentId,
      new ResidentAccountActivateRequestDto("secret-123"));

    Assert.True(activated.Succeeded);
    Assert.Equal("active", activated.Value!.PortalStatus.Code);
    Assert.True(fixture.Identity.Links.Single().IsActive);
    Assert.Equal(UserStatus.Active, fixture.Identity.Users.Single().Status);
    Assert.Equal(ResidentPortalStatus.Active, fixture.Residents.Resident.PortalStatus);

    var reset = await fixture.Service.ResetPasswordAsync(
      residentId,
      new ResidentAccountPasswordResetRequestDto("secret-456"));

    Assert.True(reset.Succeeded);
    Assert.Equal("hashed:secret-456", fixture.Identity.Users.Single().PasswordHash);
    Assert.Contains(fixture.Sessions.RevokedUserIds, userId => userId == fixture.Identity.Users.Single().Id);

    var deactivated = await fixture.Service.DeactivateAsync(
      residentId,
      new ResidentAccountLifecycleRequestDto());

    Assert.True(deactivated.Succeeded);
    Assert.Equal("disabled", deactivated.Value!.PortalStatus.Code);
    Assert.False(fixture.Identity.Links.Single().IsActive);
    Assert.Equal(UserStatus.Inactive, fixture.Identity.Users.Single().Status);

    var unlinked = await fixture.Service.UnlinkAsync(residentId, new ResidentAccountLifecycleRequestDto());

    Assert.True(unlinked.Succeeded);
    Assert.Equal("not-invited", unlinked.Value!.PortalStatus.Code);
    Assert.Null(fixture.Residents.Resident.LinkedUserId);
    Assert.Equal(5, fixture.Outbox.Envelopes.Count);
  }

  [Fact]
  public async Task LinkAsyncConnectsExistingResidentUserAndUnlinkRevokesSessions()
  {
    var fixture = ResidentAccountFixture.Create([PermissionCodes.Manage(PermissionModules.Residents)]);
    var now = DateTimeOffset.UtcNow;
    var existingUser = IdentityUser.Create(
      UserId.New(),
      "linked@example.com",
      "Linked Resident",
      UserAccountType.Resident,
      now,
      null,
      UserStatus.Active);
    fixture.Identity.Users.Add(existingUser);

    var linked = await fixture.Service.LinkAsync(
      fixture.Residents.Resident.Id.Value,
      new ResidentAccountLinkRequestDto(UserId: existingUser.Id.Value));

    Assert.True(linked.Succeeded);
    Assert.Equal(existingUser.Id.Value, linked.Value!.UserId);
    Assert.True(linked.Value.HasActiveAccountLink);
    Assert.Equal(ResidentPortalStatus.Active, fixture.Residents.Resident.PortalStatus);

    var unlinked = await fixture.Service.UnlinkAsync(
      fixture.Residents.Resident.Id.Value,
      new ResidentAccountLifecycleRequestDto());

    Assert.True(unlinked.Succeeded);
    Assert.Contains(existingUser.Id, fixture.Sessions.RevokedUserIds);
  }

  [Fact]
  public async Task InviteAsyncRequiresResidentWriteAccess()
  {
    var fixture = ResidentAccountFixture.Create([]);

    var result = await fixture.Service.InviteAsync(
      fixture.Residents.Resident.Id.Value,
      new ResidentAccountInviteRequestDto("resident@example.com"));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
    Assert.Empty(fixture.Identity.Users);
  }

  private sealed record ResidentAccountFixture(
    ResidentAccountService Service,
    FakeResidentRepository Residents,
    FakeIdentityRepository Identity,
    RecordingOutboxWriter Outbox,
    RecordingSessionInvalidator Sessions)
  {
    public static ResidentAccountFixture Create(IEnumerable<string> permissions)
    {
      var organizationId = OrganizationId.New();
      var residents = new FakeResidentRepository(organizationId);
      var identity = new FakeIdentityRepository();
      var outbox = new RecordingOutboxWriter();
      var sessions = new RecordingSessionInvalidator();
      var service = new ResidentAccountService(
        residents,
        identity,
        new FakePasswordHashService(),
        new FixedActiveOrganizationContextResolver(organizationId, permissions),
        sessions,
        new RecordingAuditWriter(),
        outbox,
        TimeProvider.System);

      return new ResidentAccountFixture(service, residents, identity, outbox, sessions);
    }
  }

  private sealed class FakeResidentRepository : IResidentRepository
  {
    public FakeResidentRepository(OrganizationId organizationId)
    {
      Resident = Resident.Create(
        EntityId.New(),
        organizationId,
        "Joao da Silva",
        null,
        "joao@example.com",
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        ResidentStatus.Active,
        ResidentPortalStatus.NotInvited,
        ResidentPrivacyOptions.None,
        null,
        null,
        DateTimeOffset.UtcNow);
    }

    public Resident Resident { get; }

    public Task AddAsync(Resident resident, CancellationToken cancellationToken = default) =>
      Task.CompletedTask;

    public Task<Resident?> FindAsync(
      EntityId residentId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Resident.Id == residentId && Resident.OrganizationId == organizationId ? Resident : null);
    }

    public Task<IReadOnlyList<Resident>> FindPotentialDuplicatesAsync(
      ResidentDuplicateWarningRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<Resident>>([]);

    public Task<PagedResultDto<Resident>> ListAsync(
      ResidentListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(new PagedResultDto<Resident>([Resident], request.Page, request.PageSize, 1));

    public Task UpdateAsync(Resident resident, CancellationToken cancellationToken = default) =>
      Task.CompletedTask;
  }

  private sealed class FakeIdentityRepository : IIdentityRepository
  {
    public List<IdentityUser> Users { get; } = [];

    public List<IdentityMembership> Memberships { get; } = [];

    public List<ResidentAccountLink> Links { get; } = [];

    public List<UserInvitation> Invitations { get; } = [];

    public Task AddMembershipAsync(IdentityMembership membership, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Memberships.Add(membership);
      return Task.CompletedTask;
    }

    public Task AddResidentAccountLinkAsync(ResidentAccountLink link, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Links.Add(link);
      return Task.CompletedTask;
    }

    public Task AddUserAsync(
      IdentityUser user,
      IEnumerable<IdentityMembership> memberships,
      UserInvitation? invitation = null,
      ResidentAccountLink? residentAccountLink = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Users.Add(user);
      Memberships.AddRange(memberships);
      if (invitation is not null)
      {
        Invitations.Add(invitation);
      }

      if (residentAccountLink is not null)
      {
        Links.Add(residentAccountLink);
      }

      return Task.CompletedTask;
    }

    public Task AddUserInvitationAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Invitations.Add(invitation);
      return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(
      string email,
      UserAccountType accountType,
      UserId? excludingUserId = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var normalized = IdentityCode.NormalizeEmail(email);
      return Task.FromResult(Users.Any(user =>
        user.AccountType == accountType &&
        user.NormalizedEmail == normalized &&
        user.Id != excludingUserId));
    }

    public Task<AdministratorDetailDto?> GetAdministratorAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<AdministratorDetailDto?>(null);

    public Task<IdentityMembership?> FindMembershipAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Memberships.FirstOrDefault(membership =>
        membership.UserId == userId &&
        membership.OrganizationId == organizationId));
    }

    public Task<IdentityOrganization?> FindOrganizationAsync(
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IdentityOrganization?>(null);

    public Task<ResidentAccountLink?> FindResidentAccountLinkAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Links.FirstOrDefault(link =>
        link.UserId == userId &&
        link.OrganizationId == organizationId &&
        link.IsActive));
    }

    public Task<ResidentAccountLink?> FindResidentAccountLinkByResidentAsync(
      EntityId residentId,
      OrganizationId organizationId,
      bool includeInactive = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Links.FirstOrDefault(link =>
        link.ResidentId == residentId &&
        link.OrganizationId == organizationId &&
        (includeInactive || link.IsActive)));
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
        "Resident",
        DateTimeOffset.UtcNow,
        isAssignable: false));
    }

    public Task<IdentityUser?> FindUserByEmailAsync(
      string email,
      UserAccountType accountType,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var normalized = IdentityCode.NormalizeEmail(email);
      return Task.FromResult(Users.FirstOrDefault(user =>
        user.AccountType == accountType &&
        user.NormalizedEmail == normalized));
    }

    public Task<IdentityUser?> FindUserByIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));
    }

    public Task<PagedResultDto<AdministratorListItemDto>> ListAdministratorsAsync(
      AdministratorListRequestDto filter,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(new PagedResultDto<AdministratorListItemDto>([], filter.Page, filter.PageSize, 0));

    public Task<IReadOnlyList<IdentityMembership>> ListMembershipsAsync(
      UserId userId,
      bool includeInactive = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<IdentityMembership>>(
        Memberships.Where(membership => membership.UserId == userId).ToArray());
    }

    public Task<IReadOnlyList<IdentityOrganization>> ListOrganizationsAsync(
      IEnumerable<OrganizationId> organizationIds,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<IdentityOrganization>>([]);

    public Task UpdateMembershipAsync(IdentityMembership membership, CancellationToken cancellationToken = default) =>
      Task.CompletedTask;

    public Task UpdateResidentAccountLinkAsync(
      ResidentAccountLink link,
      CancellationToken cancellationToken = default) =>
      Task.CompletedTask;

    public Task UpdateUserAsync(IdentityUser user, CancellationToken cancellationToken = default) =>
      Task.CompletedTask;
  }

  private sealed class FixedActiveOrganizationContextResolver : IActiveOrganizationContextResolver
  {
    private readonly ActiveOrganizationContext context;

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId, IEnumerable<string> permissions)
    {
      var membership = new OrganizationMembership(
        organizationId,
        roleCodes: [],
        permissionCodes: permissions,
        isActive: true);
      var user = new AuthenticatedUser(
        UserId.New(),
        "manager@example.com",
        "Manager",
        [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
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
    public Task WriteAsync(AuditEntryDraft entry, CancellationToken cancellationToken = default) =>
      Task.CompletedTask;
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
