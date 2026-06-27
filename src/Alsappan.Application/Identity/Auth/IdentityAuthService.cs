using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Identity.Security;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;

namespace Alsappan.Application.Identity.Auth;

public sealed class IdentityAuthService : IIdentityAuthService
{
  private readonly IIdentityRepository identityRepository;
  private readonly IPasswordHashService passwordHashService;
  private readonly IAccessTokenService accessTokenService;
  private readonly IRefreshTokenProtector refreshTokenProtector;
  private readonly IRefreshSessionStore refreshSessionStore;
  private readonly IAuthenticatedUserProvider authenticatedUserProvider;
  private readonly IRolePermissionCatalog rolePermissionCatalog;
  private readonly IAuditWriter auditWriter;
  private readonly AuthOptions authOptions;
  private readonly TimeProvider timeProvider;

  public IdentityAuthService(
    IIdentityRepository identityRepository,
    IPasswordHashService passwordHashService,
    IAccessTokenService accessTokenService,
    IRefreshTokenProtector refreshTokenProtector,
    IRefreshSessionStore refreshSessionStore,
    IAuthenticatedUserProvider authenticatedUserProvider,
    IRolePermissionCatalog rolePermissionCatalog,
    IAuditWriter auditWriter,
    AuthOptions? authOptions = null,
    TimeProvider? timeProvider = null)
  {
    this.identityRepository = identityRepository ?? throw new ArgumentNullException(nameof(identityRepository));
    this.passwordHashService = passwordHashService ?? throw new ArgumentNullException(nameof(passwordHashService));
    this.accessTokenService = accessTokenService ?? throw new ArgumentNullException(nameof(accessTokenService));
    this.refreshTokenProtector = refreshTokenProtector ?? throw new ArgumentNullException(nameof(refreshTokenProtector));
    this.refreshSessionStore = refreshSessionStore ?? throw new ArgumentNullException(nameof(refreshSessionStore));
    this.authenticatedUserProvider = authenticatedUserProvider ?? throw new ArgumentNullException(nameof(authenticatedUserProvider));
    this.rolePermissionCatalog = rolePermissionCatalog ?? throw new ArgumentNullException(nameof(rolePermissionCatalog));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.authOptions = authOptions ?? new AuthOptions();
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public Task<IdentityServiceResult<AuthSessionDto>> LoginAdminAsync(
    AdminLoginRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return LoginAsync(
      request.Email,
      request.Password,
      request.OrganizationId,
      UserAccountType.Admin,
      requestContext,
      cancellationToken);
  }

  public Task<IdentityServiceResult<AuthSessionDto>> LoginResidentAsync(
    ResidentLoginRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return LoginAsync(
      request.Email,
      request.Password,
      request.OrganizationId,
      UserAccountType.Resident,
      requestContext,
      cancellationToken);
  }

  public async Task<IdentityServiceResult<AuthSessionDto>> RefreshAsync(
    RefreshAuthSessionRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    if (string.IsNullOrWhiteSpace(request.RefreshToken))
    {
      return IdentityServiceResult<AuthSessionDto>.Invalid(
        [new ValidationFailure(nameof(request.RefreshToken), ValidationMessageKeys.Required)]);
    }

    var now = timeProvider.GetUtcNow();
    var tokenHash = refreshTokenProtector.HashToken(request.RefreshToken);
    var existingSession = await refreshSessionStore.FindByTokenHashAsync(tokenHash, cancellationToken)
      .ConfigureAwait(false);

    if (existingSession is null || !existingSession.CanRefresh(now))
    {
      return IdentityServiceResult<AuthSessionDto>.Unauthorized("auth.refreshInvalid");
    }

    var user = await identityRepository.FindUserByIdAsync(existingSession.UserId, cancellationToken)
      .ConfigureAwait(false);
    if (user is null || !user.CanMaintainSession(now))
    {
      await refreshSessionStore.RevokeAsync(
          existingSession.Id,
          now,
          "user-inactive",
          cancellationToken)
        .ConfigureAwait(false);
      return IdentityServiceResult<AuthSessionDto>.Unauthorized("auth.sessionInvalid");
    }

    var sessionContext = await BuildSessionContextAsync(
        user,
        existingSession.ActiveOrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    if (sessionContext is null)
    {
      await refreshSessionStore.RevokeAsync(
          existingSession.Id,
          now,
          "membership-invalid",
          cancellationToken)
        .ConfigureAwait(false);
      return IdentityServiceResult<AuthSessionDto>.Unauthorized("auth.sessionInvalid");
    }

    var issued = await IssueSessionAsync(
        sessionContext,
        requestContext,
        currentSessionId: existingSession.Id,
        cancellationToken)
      .ConfigureAwait(false);

    await WriteSecurityAuditAsync(
        "auth.token.refreshed",
        user,
        sessionContext.ActiveOrganizationId,
        requestContext,
        new Dictionary<string, string> { ["sessionId"] = existingSession.Id.ToString() },
        cancellationToken)
      .ConfigureAwait(false);

    return IdentityServiceResult<AuthSessionDto>.Success(issued);
  }

  public async Task<IdentityServiceResult> LogoutAsync(
    LogoutAuthSessionRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    if (string.IsNullOrWhiteSpace(request.RefreshToken))
    {
      return IdentityServiceResult.Success();
    }

    var now = timeProvider.GetUtcNow();
    var tokenHash = refreshTokenProtector.HashToken(request.RefreshToken);
    var existingSession = await refreshSessionStore.FindByTokenHashAsync(tokenHash, cancellationToken)
      .ConfigureAwait(false);

    if (existingSession is null)
    {
      return IdentityServiceResult.Success();
    }

    await refreshSessionStore.RevokeAsync(existingSession.Id, now, "logout", cancellationToken)
      .ConfigureAwait(false);

    var user = await identityRepository.FindUserByIdAsync(existingSession.UserId, cancellationToken)
      .ConfigureAwait(false);
    if (user is not null)
    {
      await WriteSecurityAuditAsync(
          "auth.logout",
          user,
          existingSession.ActiveOrganizationId,
          requestContext,
          new Dictionary<string, string> { ["sessionId"] = existingSession.Id.ToString() },
          cancellationToken)
        .ConfigureAwait(false);
    }

    return IdentityServiceResult.Success();
  }

  public async Task<IdentityServiceResult<CurrentUserDto>> GetCurrentUserAsync(
    CancellationToken cancellationToken = default)
  {
    var authenticatedUser = await authenticatedUserProvider.GetCurrentUserAsync(cancellationToken)
      .ConfigureAwait(false);
    if (authenticatedUser is null)
    {
      return IdentityServiceResult<CurrentUserDto>.Unauthorized("auth.unauthenticated");
    }

    var user = await identityRepository.FindUserByIdAsync(authenticatedUser.UserId, cancellationToken)
      .ConfigureAwait(false);
    var now = timeProvider.GetUtcNow();
    if (user is null || !user.CanMaintainSession(now))
    {
      return IdentityServiceResult<CurrentUserDto>.Unauthorized("auth.sessionInvalid");
    }

    var activeOrganizationId = ResolveActiveOrganizationId(authenticatedUser);
    var sessionContext = await BuildSessionContextAsync(user, activeOrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (sessionContext is null)
    {
      return IdentityServiceResult<CurrentUserDto>.Unauthorized("auth.sessionInvalid");
    }

    return IdentityServiceResult<CurrentUserDto>.Success(BuildCurrentUserDto(sessionContext));
  }

  public async Task<IdentityServiceResult<AuthSessionDto>> SwitchOrganizationAsync(
    SwitchOrganizationRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    if (request.OrganizationId == Guid.Empty)
    {
      return IdentityServiceResult<AuthSessionDto>.Invalid(
        [new ValidationFailure(nameof(request.OrganizationId), ValidationMessageKeys.InvalidId)]);
    }

    var authenticatedUser = await authenticatedUserProvider.GetCurrentUserAsync(cancellationToken)
      .ConfigureAwait(false);
    if (authenticatedUser is null)
    {
      return IdentityServiceResult<AuthSessionDto>.Unauthorized("auth.unauthenticated");
    }

    var user = await identityRepository.FindUserByIdAsync(authenticatedUser.UserId, cancellationToken)
      .ConfigureAwait(false);
    var now = timeProvider.GetUtcNow();
    if (user is null || !user.CanMaintainSession(now))
    {
      return IdentityServiceResult<AuthSessionDto>.Unauthorized("auth.sessionInvalid");
    }

    var requestedOrganizationId = new OrganizationId(request.OrganizationId);
    var sessionContext = await BuildSessionContextAsync(user, requestedOrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (sessionContext is null)
    {
      return IdentityServiceResult<AuthSessionDto>.Forbidden("auth.organizationDenied");
    }

    var session = await IssueSessionAsync(sessionContext, requestContext, null, cancellationToken)
      .ConfigureAwait(false);

    await WriteSecurityAuditAsync(
        "auth.organization.switched",
        user,
        requestedOrganizationId,
        requestContext,
        new Dictionary<string, string> { ["organizationId"] = requestedOrganizationId.ToString() },
        cancellationToken)
      .ConfigureAwait(false);

    return IdentityServiceResult<AuthSessionDto>.Success(session);
  }

  private async Task<IdentityServiceResult<AuthSessionDto>> LoginAsync(
    string? email,
    string? password,
    Guid? organizationId,
    UserAccountType accountType,
    IdentityRequestContext? requestContext,
    CancellationToken cancellationToken)
  {
    var validationFailures = ValidateLogin(email, password, organizationId).ToArray();
    if (validationFailures.Length > 0)
    {
      return IdentityServiceResult<AuthSessionDto>.Invalid(validationFailures);
    }

    var now = timeProvider.GetUtcNow();
    var user = await identityRepository.FindUserByEmailAsync(email!, accountType, cancellationToken)
      .ConfigureAwait(false);
    if (user is null)
    {
      return IdentityServiceResult<AuthSessionDto>.Unauthorized();
    }

    var requestedOrganizationId = organizationId.HasValue
      ? new OrganizationId(organizationId.Value)
      : (OrganizationId?)null;

    if (!user.CanAttemptPasswordLogin(now))
    {
      await WriteSecurityAuditAsync(
          "auth.login.failed",
          user,
          requestedOrganizationId,
          requestContext,
          new Dictionary<string, string> { ["reason"] = "status" },
          cancellationToken)
        .ConfigureAwait(false);
      return IdentityServiceResult<AuthSessionDto>.Unauthorized();
    }

    var verification = passwordHashService.VerifyPassword(user, password!, user.PasswordHash!);
    if (verification == PasswordVerificationResult.Failed)
    {
      user.RecordFailedLogin(now);
      await identityRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
      await WriteSecurityAuditAsync(
          "auth.login.failed",
          user,
          requestedOrganizationId,
          requestContext,
          new Dictionary<string, string> { ["reason"] = "credentials" },
          cancellationToken)
        .ConfigureAwait(false);
      return IdentityServiceResult<AuthSessionDto>.Unauthorized();
    }

    var sessionContext = await BuildSessionContextAsync(user, requestedOrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (sessionContext is null)
    {
      await WriteSecurityAuditAsync(
          "auth.login.failed",
          user,
          requestedOrganizationId,
          requestContext,
          new Dictionary<string, string> { ["reason"] = "membership" },
          cancellationToken)
        .ConfigureAwait(false);
      return IdentityServiceResult<AuthSessionDto>.Unauthorized();
    }

    user.RecordSuccessfulLogin(now);
    if (verification == PasswordVerificationResult.SuccessRehashNeeded)
    {
      user.SetPasswordHash(passwordHashService.HashPassword(user, password!), now, user.Id);
    }

    await identityRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

    var session = await IssueSessionAsync(sessionContext, requestContext, null, cancellationToken)
      .ConfigureAwait(false);

    await WriteSecurityAuditAsync(
        "auth.login.succeeded",
        user,
        sessionContext.ActiveOrganizationId,
        requestContext,
        new Dictionary<string, string> { ["accountType"] = ToAccountTypeCode(accountType) },
        cancellationToken)
      .ConfigureAwait(false);

    return IdentityServiceResult<AuthSessionDto>.Success(session);
  }

  private async Task<UserSessionContext?> BuildSessionContextAsync(
    IdentityUser user,
    OrganizationId? requestedOrganizationId,
    CancellationToken cancellationToken)
  {
    var memberships = (await identityRepository.ListMembershipsAsync(user.Id, cancellationToken: cancellationToken)
        .ConfigureAwait(false))
      .Where(membership => membership.IsActive)
      .OrderBy(membership => membership.OrganizationId.Value)
      .ToArray();

    if (memberships.Length == 0)
    {
      return null;
    }

    IdentityMembership? activeMembership;
    if (requestedOrganizationId.HasValue)
    {
      activeMembership = memberships.FirstOrDefault(membership =>
        membership.OrganizationId == requestedOrganizationId.Value);
    }
    else if (user.AccountType == UserAccountType.Resident)
    {
      activeMembership = await FindFirstResidentMembershipAsync(user.Id, memberships, cancellationToken)
        .ConfigureAwait(false);
    }
    else
    {
      activeMembership = memberships[0];
    }

    if (activeMembership is null)
    {
      return null;
    }

    if (user.AccountType == UserAccountType.Resident)
    {
      var link = await identityRepository.FindResidentAccountLinkAsync(
          user.Id,
          activeMembership.OrganizationId,
          cancellationToken)
        .ConfigureAwait(false);
      if (link is null || !link.IsActive)
      {
        return null;
      }
    }

    var organizations = await identityRepository.ListOrganizationsAsync(
        memberships.Select(membership => membership.OrganizationId),
        cancellationToken)
      .ConfigureAwait(false);

    if (!organizations.Any(organization => organization.Id == activeMembership.OrganizationId))
    {
      return null;
    }

    return new UserSessionContext(user, activeMembership.OrganizationId, memberships, organizations);
  }

  private async Task<IdentityMembership?> FindFirstResidentMembershipAsync(
    UserId userId,
    IReadOnlyList<IdentityMembership> memberships,
    CancellationToken cancellationToken)
  {
    foreach (var membership in memberships)
    {
      var link = await identityRepository.FindResidentAccountLinkAsync(
          userId,
          membership.OrganizationId,
          cancellationToken)
        .ConfigureAwait(false);

      if (link?.IsActive == true)
      {
        return membership;
      }
    }

    return null;
  }

  private async Task<AuthSessionDto> IssueSessionAsync(
    UserSessionContext sessionContext,
    IdentityRequestContext? requestContext,
    EntityId? currentSessionId,
    CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    var refreshToken = refreshTokenProtector.CreateToken();
    var refreshSessionId = EntityId.New();
    var refreshSession = new RefreshSessionRecord(
      refreshSessionId,
      sessionContext.User.Id,
      refreshToken.TokenHash,
      now,
      now.AddDays(GetRefreshTokenDays()),
      sessionContext.ActiveOrganizationId,
      requestContext?.UserAgent,
      requestContext?.IpAddress);

    if (currentSessionId.HasValue)
    {
      await refreshSessionStore.RotateAsync(
          currentSessionId.Value,
          refreshSession,
          now,
          cancellationToken)
        .ConfigureAwait(false);
    }
    else
    {
      await refreshSessionStore.CreateAsync(refreshSession, cancellationToken).ConfigureAwait(false);
    }

    var tokenMemberships = BuildTokenMemberships(sessionContext);
    var issuedAccessToken = accessTokenService.IssueToken(new AccessTokenDescriptor(
      sessionContext.User.Id,
      tokenMemberships,
      sessionContext.User.Email,
      sessionContext.User.DisplayName,
      refreshSessionId));

    return new AuthSessionDto(
      issuedAccessToken.Token,
      issuedAccessToken.TokenType,
      issuedAccessToken.ExpiresAt,
      refreshToken.Token,
      BuildCurrentUserDto(sessionContext));
  }

  private CurrentUserDto BuildCurrentUserDto(UserSessionContext sessionContext)
  {
    var organizationsById = sessionContext.Organizations.ToDictionary(
      organization => organization.Id,
      organization => organization);
    var organizationSummaries = sessionContext.Memberships
      .Where(membership => organizationsById.ContainsKey(membership.OrganizationId))
      .Select(membership =>
      {
        var organization = organizationsById[membership.OrganizationId];
        return new OrganizationContextDto(
          organization.Id.Value,
          organization.Slug,
          organization.Name,
          organization.DisplayName,
          organization.Locale,
          organization.Currency,
          membership.RoleCodes,
          BuildEffectivePermissionCodes(membership));
      })
      .ToArray();

    var activeMembership = sessionContext.Memberships.Single(membership =>
      membership.OrganizationId == sessionContext.ActiveOrganizationId);

    return new CurrentUserDto(
      sessionContext.User.Id.Value,
      sessionContext.User.Email,
      sessionContext.User.DisplayName ?? sessionContext.User.Email,
      ToAccountTypeCode(sessionContext.User.AccountType),
      sessionContext.ActiveOrganizationId.Value,
      organizationSummaries,
      BuildEffectivePermissionCodes(activeMembership));
  }

  private OrganizationMembership[] BuildTokenMemberships(UserSessionContext sessionContext) =>
    sessionContext.Memberships
      .Select(membership => new OrganizationMembership(
        membership.OrganizationId,
        membership.RoleCodes,
        BuildEffectivePermissionCodes(membership),
        membership.OrganizationId == sessionContext.ActiveOrganizationId))
      .ToArray();

  private string[] BuildEffectivePermissionCodes(IdentityMembership membership)
  {
    var permissions = new HashSet<string>(membership.PermissionCodes, StringComparer.OrdinalIgnoreCase);
    permissions.UnionWith(rolePermissionCatalog.GetPermissionsForRoles(membership.RoleCodes));

    return permissions
      .Order(StringComparer.OrdinalIgnoreCase)
      .ToArray();
  }

  private async Task WriteSecurityAuditAsync(
    string action,
    IdentityUser user,
    OrganizationId? organizationId,
    IdentityRequestContext? requestContext,
    IReadOnlyDictionary<string, string>? context,
    CancellationToken cancellationToken)
  {
    var auditOrganizationId = organizationId ?? await FindFirstAuditOrganizationAsync(user, cancellationToken)
      .ConfigureAwait(false);
    if (!auditOrganizationId.HasValue)
    {
      return;
    }

    var auditContext = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["accountType"] = ToAccountTypeCode(user.AccountType),
    };

    if (!string.IsNullOrWhiteSpace(requestContext?.IpAddress))
    {
      auditContext["ipAddress"] = requestContext.IpAddress.Trim();
    }

    if (!string.IsNullOrWhiteSpace(requestContext?.UserAgent))
    {
      auditContext["userAgent"] = requestContext.UserAgent.Trim();
    }

    if (context is not null)
    {
      foreach (var (key, value) in context)
      {
        auditContext[key] = value;
      }
    }

    await auditWriter.WriteAsync(
        new AuditEntryDraft(
          auditOrganizationId.Value,
          action,
          AuditEntryCategory.Security,
          user.AccountType == UserAccountType.Resident
            ? EventActor.Resident(user.Id, user.DisplayName)
            : EventActor.User(user.Id, user.DisplayName),
          EntityReference.FromGuid("identityUser", user.Id.Value, user.Email),
          timeProvider.GetUtcNow(),
          context: auditContext,
          correlationId: requestContext?.CorrelationId),
        cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task<OrganizationId?> FindFirstAuditOrganizationAsync(
    IdentityUser user,
    CancellationToken cancellationToken)
  {
    var memberships = await identityRepository.ListMembershipsAsync(
        user.Id,
        includeInactive: true,
        cancellationToken)
      .ConfigureAwait(false);

    return memberships
      .OrderBy(membership => membership.OrganizationId.Value)
      .Select(membership => membership.OrganizationId)
      .Cast<OrganizationId?>()
      .FirstOrDefault();
  }

  private int GetRefreshTokenDays() => authOptions.RefreshTokenDays > 0
    ? authOptions.RefreshTokenDays
    : 30;

  private static OrganizationId? ResolveActiveOrganizationId(AuthenticatedUser user) =>
    user.Memberships.FirstOrDefault(membership => membership.IsActive)?.OrganizationId
    ?? (user.Memberships.Count == 1 ? user.Memberships[0].OrganizationId : null);

  private static IEnumerable<ValidationFailure> ValidateLogin(
    string? email,
    string? password,
    Guid? organizationId)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      yield return new ValidationFailure(nameof(email), ValidationMessageKeys.Required);
    }
    else if (!email.Contains('@', StringComparison.Ordinal))
    {
      yield return new ValidationFailure(nameof(email), ValidationMessageKeys.Email);
    }

    if (string.IsNullOrWhiteSpace(password))
    {
      yield return new ValidationFailure(nameof(password), ValidationMessageKeys.Required);
    }

    if (organizationId == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(organizationId), ValidationMessageKeys.InvalidId);
    }
  }

  private static string ToAccountTypeCode(UserAccountType accountType) =>
    accountType == UserAccountType.Resident ? "resident" : "admin";

  private sealed record UserSessionContext(
    IdentityUser User,
    OrganizationId ActiveOrganizationId,
    IReadOnlyList<IdentityMembership> Memberships,
    IReadOnlyList<IdentityOrganization> Organizations);
}
