using System.Security.Cryptography;
using System.Text;
using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Identity.Security;
using Alsappan.Application.Identity.Sessions;
using Alsappan.Application.Residents.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Residents;

namespace Alsappan.Application.Residents;

public sealed class ResidentAccountService : IResidentAccountService
{
  private readonly IResidentRepository residentRepository;
  private readonly IIdentityRepository identityRepository;
  private readonly IPasswordHashService passwordHashService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IIdentitySessionInvalidator sessionInvalidator;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public ResidentAccountService(
    IResidentRepository residentRepository,
    IIdentityRepository identityRepository,
    IPasswordHashService passwordHashService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IIdentitySessionInvalidator sessionInvalidator,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider? timeProvider = null)
  {
    this.residentRepository = residentRepository ?? throw new ArgumentNullException(nameof(residentRepository));
    this.identityRepository = identityRepository ?? throw new ArgumentNullException(nameof(identityRepository));
    this.passwordHashService = passwordHashService ?? throw new ArgumentNullException(nameof(passwordHashService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.sessionInvalidator = sessionInvalidator ?? throw new ArgumentNullException(nameof(sessionInvalidator));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<ResidentAccountAccessDto>> InviteAsync(
    Guid residentId,
    ResidentAccountInviteRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateResidentId(residentId).Concat(ValidateEmail(request.Email)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Invalid(validation);
    }

    var context = await ResolveManagementContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var resident = await FindResidentAsync(residentId, context, cancellationToken).ConfigureAwait(false);
    if (resident is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var existingLink = await identityRepository.FindResidentAccountLinkByResidentAsync(
        resident.Id,
        context.OrganizationId,
        includeInactive: true,
        cancellationToken)
      .ConfigureAwait(false);
    if (existingLink is not null)
    {
      return Conflict("residentAccount", "validation.residentPortalLinked");
    }

    if (await identityRepository.EmailExistsAsync(
        request.Email,
        UserAccountType.Resident,
        cancellationToken: cancellationToken)
      .ConfigureAwait(false))
    {
      return Conflict(nameof(request.Email), "validation.emailTaken");
    }

    var residentRole = await identityRepository.FindRoleByCodeAsync(
        RoleCodes.ResidentUser,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    if (residentRole is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Invalid(
        [new ValidationFailure("role", "validation.residentRole")]);
    }

    var now = timeProvider.GetUtcNow();
    var invitationToken = CreateInvitationToken();
    var user = IdentityUser.Create(
      UserId.New(),
      request.Email,
      string.IsNullOrWhiteSpace(request.DisplayName) ? resident.FullName : request.DisplayName,
      UserAccountType.Resident,
      now,
      context.UserId,
      UserStatus.Invited);
    var membership = IdentityMembership.Create(
      EntityId.New(),
      context.OrganizationId,
      user.Id,
      [RoleCodes.ResidentUser],
      now,
      context.UserId);
    var invitation = new UserInvitation(
      EntityId.New(),
      context.OrganizationId,
      user.Email,
      user.DisplayName ?? resident.FullName,
      residentRole.Id,
      HashToken(invitationToken),
      now.AddDays(7),
      now,
      context.UserId);
    var link = new ResidentAccountLink(
      EntityId.New(),
      context.OrganizationId,
      user.Id,
      resident.Id,
      now,
      context.UserId,
      isActive: false);

    resident.LinkUser(user.Id, ResidentPortalStatus.Invited, now, context.UserId);

    await identityRepository.AddUserAsync(user, [membership], invitation, link, cancellationToken)
      .ConfigureAwait(false);
    await residentRepository.UpdateAsync(resident, cancellationToken).ConfigureAwait(false);
    await WriteAccountEventAsync("resident.account.invited", resident, user, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ResidentAccountAccessDto>.Success(
      ToDto(resident, user, link, locale, invitationToken));
  }

  public async Task<ApplicationOperationResult<ResidentAccountAccessDto>> ActivateAsync(
    Guid residentId,
    ResidentAccountActivateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateResidentId(residentId).Concat(ValidatePassword(request.Password)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Invalid(validation);
    }

    return await MutateLinkedAccountAsync(
        residentId,
        locale,
        "resident.account.activated",
        async (resident, user, membership, link, context, ct) =>
        {
          var now = timeProvider.GetUtcNow();
          user.SetPasswordHash(passwordHashService.HashPassword(user, request.Password), now, context.UserId);
          if (user.Status != UserStatus.Active)
          {
            user.Reactivate(now, context.UserId);
          }

          var createdMembership = membership is null;
          membership ??= IdentityMembership.Create(
            EntityId.New(),
            context.OrganizationId,
            user.Id,
            [RoleCodes.ResidentUser],
            now,
            context.UserId);
          if (!membership.IsActive)
          {
            membership.Reactivate(now, context.UserId);
          }

          link.Reactivate(now, context.UserId);
          resident.LinkUser(user.Id, ResidentPortalStatus.Active, now, context.UserId);

          await identityRepository.UpdateUserAsync(user, ct).ConfigureAwait(false);
          if (createdMembership)
          {
            await identityRepository.AddMembershipAsync(membership, ct).ConfigureAwait(false);
          }
          else
          {
            await identityRepository.UpdateMembershipAsync(membership, ct).ConfigureAwait(false);
          }

          await identityRepository.UpdateResidentAccountLinkAsync(link, ct).ConfigureAwait(false);
          await residentRepository.UpdateAsync(resident, ct).ConfigureAwait(false);
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<ResidentAccountAccessDto>> DeactivateAsync(
    Guid residentId,
    ResidentAccountLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateLinkedAccountAsync(
        residentId,
        locale,
        "resident.account.deactivated",
        async (resident, user, membership, link, context, ct) =>
        {
          var now = timeProvider.GetUtcNow();
          user.Deactivate(now, context.UserId);
          membership?.Deactivate(now, context.UserId);
          link.Deactivate(now, context.UserId);
          resident.LinkUser(user.Id, ResidentPortalStatus.Disabled, now, context.UserId);

          await SaveAccountStateAsync(user, membership, link, resident, ct).ConfigureAwait(false);
          await sessionInvalidator.RevokeUserSessionsAsync(user.Id, now, "resident-account-deactivated", ct)
            .ConfigureAwait(false);
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<ResidentAccountAccessDto>> ResetPasswordAsync(
    Guid residentId,
    ResidentAccountPasswordResetRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidatePassword(request.Password).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Invalid(validation);
    }

    return await MutateLinkedAccountAsync(
        residentId,
        locale,
        "resident.account.password-reset",
        async (resident, user, membership, link, context, ct) =>
        {
          if (resident.PortalStatus != ResidentPortalStatus.Active || !link.IsActive)
          {
            throw new InvalidOperationException("Only active resident accounts can reset passwords.");
          }

          var now = timeProvider.GetUtcNow();
          user.SetPasswordHash(passwordHashService.HashPassword(user, request.Password), now, context.UserId);
          await identityRepository.UpdateUserAsync(user, ct).ConfigureAwait(false);
          await sessionInvalidator.RevokeUserSessionsAsync(user.Id, now, "resident-password-reset", ct)
            .ConfigureAwait(false);
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<ResidentAccountAccessDto>> LinkAsync(
    Guid residentId,
    ResidentAccountLinkRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateResidentId(residentId).Concat(ValidateLinkRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Invalid(validation);
    }

    var context = await ResolveManagementContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var resident = await FindResidentAsync(residentId, context, cancellationToken).ConfigureAwait(false);
    if (resident is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var existingLink = await identityRepository.FindResidentAccountLinkByResidentAsync(
        resident.Id,
        context.OrganizationId,
        includeInactive: true,
        cancellationToken)
      .ConfigureAwait(false);
    if (existingLink is not null && existingLink.IsActive)
    {
      return Conflict("residentAccount", "validation.residentPortalLinked");
    }

    var user = await ResolveResidentUserAsync(request, cancellationToken).ConfigureAwait(false);
    if (user is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (existingLink is not null && existingLink.UserId != user.Id)
    {
      return Conflict("residentAccount", "validation.residentPortalLinked");
    }

    var now = timeProvider.GetUtcNow();
    var membership = await identityRepository.FindMembershipAsync(user.Id, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (membership is null)
    {
      membership = IdentityMembership.Create(
        EntityId.New(),
        context.OrganizationId,
        user.Id,
        [RoleCodes.ResidentUser],
        now,
        context.UserId);
      await identityRepository.AddMembershipAsync(membership, cancellationToken).ConfigureAwait(false);
    }
    else
    {
      var roleCodes = membership.RoleCodes
        .Append(RoleCodes.ResidentUser)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
      membership.AssignAccess(roleCodes, membership.PermissionCodes, now, context.UserId);
      if (!membership.IsActive)
      {
        membership.Reactivate(now, context.UserId);
      }

      await identityRepository.UpdateMembershipAsync(membership, cancellationToken).ConfigureAwait(false);
    }

    var link = existingLink;
    if (link is null)
    {
      link = new ResidentAccountLink(
        EntityId.New(),
        context.OrganizationId,
        user.Id,
        resident.Id,
        now,
        context.UserId,
        isActive: true);
      await identityRepository.AddResidentAccountLinkAsync(link, cancellationToken).ConfigureAwait(false);
    }
    else
    {
      link.Reactivate(now, context.UserId);
      await identityRepository.UpdateResidentAccountLinkAsync(link, cancellationToken).ConfigureAwait(false);
    }

    var portalStatus = user.Status == UserStatus.Active
      ? ResidentPortalStatus.Active
      : ResidentPortalStatus.Invited;
    resident.LinkUser(user.Id, portalStatus, now, context.UserId);
    await residentRepository.UpdateAsync(resident, cancellationToken).ConfigureAwait(false);
    await WriteAccountEventAsync("resident.account.linked", resident, user, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ResidentAccountAccessDto>.Success(ToDto(resident, user, link, locale));
  }

  public async Task<ApplicationOperationResult<ResidentAccountAccessDto>> UnlinkAsync(
    Guid residentId,
    ResidentAccountLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateLinkedAccountAsync(
        residentId,
        locale,
        "resident.account.unlinked",
        async (resident, user, membership, link, context, ct) =>
        {
          var now = timeProvider.GetUtcNow();
          membership?.Deactivate(now, context.UserId);
          link.Deactivate(now, context.UserId);
          resident.UnlinkUser(now, context.UserId);

          await SaveAccountStateAsync(user, membership, link, resident, ct).ConfigureAwait(false);
          await sessionInvalidator.RevokeUserSessionsAsync(user.Id, now, "resident-account-unlinked", ct)
            .ConfigureAwait(false);
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task<ApplicationOperationResult<ResidentAccountAccessDto>> MutateLinkedAccountAsync(
    Guid residentId,
    string? locale,
    string eventName,
    Func<Resident, IdentityUser, IdentityMembership?, ResidentAccountLink, ActiveOrganizationContext, CancellationToken, Task> mutate,
    CancellationToken cancellationToken)
  {
    var validation = ValidateResidentId(residentId).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Invalid(validation);
    }

    var context = await ResolveManagementContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var resident = await FindResidentAsync(residentId, context, cancellationToken).ConfigureAwait(false);
    if (resident is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var link = await identityRepository.FindResidentAccountLinkByResidentAsync(
        resident.Id,
        context.OrganizationId,
        includeInactive: true,
        cancellationToken)
      .ConfigureAwait(false);
    if (link is null)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var user = await identityRepository.FindUserByIdAsync(link.UserId, cancellationToken).ConfigureAwait(false);
    if (user is null || user.AccountType != UserAccountType.Resident)
    {
      return ApplicationOperationResult<ResidentAccountAccessDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var membership = await identityRepository.FindMembershipAsync(user.Id, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);

    try
    {
      await mutate(resident, user, membership, link, context, cancellationToken).ConfigureAwait(false);
    }
    catch (InvalidOperationException)
    {
      return Conflict("residentAccount", "validation.residentPortalState");
    }

    await WriteAccountEventAsync(eventName, resident, user, context, cancellationToken).ConfigureAwait(false);
    return ApplicationOperationResult<ResidentAccountAccessDto>.Success(ToDto(resident, user, link, locale));
  }

  private async Task SaveAccountStateAsync(
    IdentityUser user,
    IdentityMembership? membership,
    ResidentAccountLink link,
    Resident resident,
    CancellationToken cancellationToken)
  {
    await identityRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
    if (membership is not null)
    {
      await identityRepository.UpdateMembershipAsync(membership, cancellationToken).ConfigureAwait(false);
    }

    await identityRepository.UpdateResidentAccountLinkAsync(link, cancellationToken).ConfigureAwait(false);
    await residentRepository.UpdateAsync(resident, cancellationToken).ConfigureAwait(false);
  }

  private async Task<ActiveOrganizationContext?> ResolveManagementContextAsync(CancellationToken cancellationToken)
  {
    var result = await activeOrganizationContextResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
    if (!result.Succeeded || result.Context is null)
    {
      return null;
    }

    var permissions = result.Context.Membership.PermissionCodes;
    var roles = result.Context.Membership.RoleCodes;
    var isAllowed = permissions.Contains(PermissionCodes.Wildcard, StringComparer.OrdinalIgnoreCase) ||
      permissions.Contains(PermissionCodes.Write(PermissionModules.Residents), StringComparer.OrdinalIgnoreCase) ||
      permissions.Contains(PermissionCodes.Manage(PermissionModules.Residents), StringComparer.OrdinalIgnoreCase) ||
      roles.Contains(RoleCodes.OrganizationAdmin, StringComparer.OrdinalIgnoreCase) ||
      roles.Contains(RoleCodes.OrganizationManager, StringComparer.OrdinalIgnoreCase);

    return isAllowed ? result.Context : null;
  }

  private async Task<Resident?> FindResidentAsync(
    Guid residentId,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken) =>
    await residentRepository.FindAsync(
        new EntityId(residentId),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<IdentityUser?> ResolveResidentUserAsync(
    ResidentAccountLinkRequestDto request,
    CancellationToken cancellationToken)
  {
    if (request.UserId.HasValue)
    {
      var user = await identityRepository.FindUserByIdAsync(new UserId(request.UserId.Value), cancellationToken)
        .ConfigureAwait(false);
      return user?.AccountType == UserAccountType.Resident ? user : null;
    }

    if (!string.IsNullOrWhiteSpace(request.Email))
    {
      return await identityRepository.FindUserByEmailAsync(
          request.Email,
          UserAccountType.Resident,
          cancellationToken)
        .ConfigureAwait(false);
    }

    return null;
  }

  private async Task WriteAccountEventAsync(
    string eventName,
    Resident resident,
    IdentityUser user,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    var subject = EntityReference.FromGuid("resident", resident.Id.Value, resident.FullName);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var related = new[]
    {
      EntityReference.FromGuid("identityUser", user.Id.Value, user.Email)
    };
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["portalStatus"] = ResidentCatalog.ToPortalStatusCode(resident.PortalStatus),
      ["userId"] = user.Id.Value.ToString("D"),
      ["email"] = user.Email
    };

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "residents",
      eventName,
      now,
      actor,
      subject,
      ModuleEventConsumer.Audit | ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications,
      data,
      related);

    await auditWriter.WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static ResidentAccountAccessDto ToDto(
    Resident resident,
    IdentityUser? user,
    ResidentAccountLink? link,
    string? locale,
    string? invitationToken = null) =>
    new(
      resident.Id.Value,
      user?.Id.Value ?? resident.LinkedUserId?.Value,
      user?.Email,
      user?.DisplayName,
      ResidentCatalog.GetPortalStatusLabel(resident.PortalStatus, locale),
      resident.HasPortalAccess && link?.IsActive == true && user?.Status == UserStatus.Active,
      link?.IsActive == true,
      invitationToken,
      resident.UpdatedAt);

  private static IEnumerable<ValidationFailure> ValidateResidentId(Guid residentId)
  {
    if (residentId == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(residentId), ValidationMessageKeys.InvalidId);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateEmail(string? email)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      yield return new ValidationFailure(nameof(email), ValidationMessageKeys.Required);
    }
    else if (!email.Contains('@', StringComparison.Ordinal))
    {
      yield return new ValidationFailure(nameof(email), ValidationMessageKeys.Email);
    }
  }

  private static IEnumerable<ValidationFailure> ValidatePassword(string? password)
  {
    if (string.IsNullOrWhiteSpace(password))
    {
      yield return new ValidationFailure(nameof(password), ValidationMessageKeys.Required);
    }
    else if (password.Length < 8)
    {
      yield return new ValidationFailure(nameof(password), "validation.password");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateLinkRequest(ResidentAccountLinkRequestDto request)
  {
    if (request.UserId == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(request.UserId), ValidationMessageKeys.InvalidId);
    }

    if (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.Email))
    {
      yield return new ValidationFailure(nameof(request.Email), ValidationMessageKeys.Required);
    }

    if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@', StringComparison.Ordinal))
    {
      yield return new ValidationFailure(nameof(request.Email), ValidationMessageKeys.Email);
    }
  }

  private static ApplicationOperationResult<ResidentAccountAccessDto> Conflict(string field, string messageKey) =>
    ApplicationOperationResult<ResidentAccountAccessDto>.Failed(
      ApplicationOperationFailure.Conflict,
      new Dictionary<string, string[]> { [field] = [messageKey] });

  private static string CreateInvitationToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

  private static string HashToken(string token) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
