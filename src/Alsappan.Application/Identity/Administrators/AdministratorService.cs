using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Identity.Security;
using Alsappan.Application.Identity.Sessions;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;

namespace Alsappan.Application.Identity.Administrators;

public sealed class AdministratorService : IAdministratorService
{
  private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

  private readonly IIdentityRepository identityRepository;
  private readonly IPasswordHashService passwordHashService;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IIdentitySessionInvalidator sessionInvalidator;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public AdministratorService(
    IIdentityRepository identityRepository,
    IPasswordHashService passwordHashService,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IIdentitySessionInvalidator sessionInvalidator,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider? timeProvider = null)
  {
    this.identityRepository = identityRepository ?? throw new ArgumentNullException(nameof(identityRepository));
    this.passwordHashService = passwordHashService ?? throw new ArgumentNullException(nameof(passwordHashService));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.sessionInvalidator = sessionInvalidator ?? throw new ArgumentNullException(nameof(sessionInvalidator));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<IdentityOperationResult<PagedResultDto<AdministratorListItemDto>>> ListAsync(
    AdministratorListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(
        PermissionCodes.Read(PermissionModules.Administrators),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return Failed<PagedResultDto<AdministratorListItemDto>>(IdentityOperationFailure.Forbidden);
    }

    var page = await identityRepository.ListAdministratorsAsync(
        request,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    return IdentityOperationResult<PagedResultDto<AdministratorListItemDto>>.Success(page);
  }

  public async Task<IdentityOperationResult<AdministratorDetailDto>> GetAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    if (id == Guid.Empty)
    {
      return Invalid<AdministratorDetailDto>([new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId)]);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Read(PermissionModules.Administrators),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return Failed<AdministratorDetailDto>(IdentityOperationFailure.Forbidden);
    }

    var detail = await identityRepository.GetAdministratorAsync(
        new UserId(id),
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);

    return detail is null
      ? Failed<AdministratorDetailDto>(IdentityOperationFailure.NotFound)
      : IdentityOperationResult<AdministratorDetailDto>.Success(detail);
  }

  public async Task<IdentityOperationResult<AdministratorDetailDto>> CreateAsync(
    AdministratorCreateRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(
        PermissionCodes.Write(PermissionModules.Administrators),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return Failed<AdministratorDetailDto>(IdentityOperationFailure.Forbidden);
    }

    var failures = ValidateAdministratorRequest(request.Email, request.DisplayName, request.RoleCodes).ToList();
    var roleCodes = IdentityRoleCatalog.NormalizeAdministratorRoles(request.RoleCodes);
    if (roleCodes.Length == 0)
    {
      failures.Add(new ValidationFailure(nameof(request.RoleCodes), "validation.role"));
    }

    if (!string.IsNullOrWhiteSpace(request.TemporaryPassword))
    {
      failures.AddRange(ValidatePassword(request.TemporaryPassword));
    }

    if (failures.Count > 0)
    {
      return Invalid<AdministratorDetailDto>(failures);
    }

    var exists = await identityRepository.EmailExistsAsync(
        request.Email,
        UserAccountType.Admin,
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);
    if (exists)
    {
      return Failed<AdministratorDetailDto>(IdentityOperationFailure.Conflict);
    }

    var now = timeProvider.GetUtcNow();
    var user = IdentityUser.Create(
      UserId.New(),
      request.Email,
      request.DisplayName,
      UserAccountType.Admin,
      now,
      context.UserId,
      string.IsNullOrWhiteSpace(request.TemporaryPassword) ? UserStatus.Invited : UserStatus.Active);

    if (!string.IsNullOrWhiteSpace(request.TemporaryPassword))
    {
      user.SetPasswordHash(
        passwordHashService.HashPassword(user, request.TemporaryPassword),
        now,
        context.UserId);
    }

    var membership = IdentityMembership.Create(
      EntityId.New(),
      context.OrganizationId,
      user.Id,
      roleCodes,
      now,
      context.UserId);

    var invitation = string.IsNullOrWhiteSpace(request.TemporaryPassword)
      ? new UserInvitation(
        EntityId.New(),
        context.OrganizationId,
        user.Email,
        user.DisplayName ?? user.Email,
        EntityId.New(),
        Guid.NewGuid().ToString("N"),
        now.Add(InvitationLifetime),
        now,
        context.UserId)
      : null;

    await identityRepository.AddUserAsync(user, [membership], invitation, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
    await WriteSecuritySideEffectsAsync("administrators.invited", context, user, cancellationToken)
      .ConfigureAwait(false);

    var detail = await identityRepository.GetAdministratorAsync(user.Id, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    return IdentityOperationResult<AdministratorDetailDto>.Success(detail!);
  }

  public async Task<IdentityOperationResult<AdministratorDetailDto>> UpdateAsync(
    Guid id,
    AdministratorUpdateRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    if (id == Guid.Empty)
    {
      return Invalid<AdministratorDetailDto>([new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId)]);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Manage(PermissionModules.Administrators),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return Failed<AdministratorDetailDto>(IdentityOperationFailure.Forbidden);
    }

    var userId = new UserId(id);
    var user = await identityRepository.FindUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
    var membership = await identityRepository.FindMembershipAsync(userId, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (user is null || user.AccountType != UserAccountType.Admin || membership is null)
    {
      return Failed<AdministratorDetailDto>(IdentityOperationFailure.NotFound);
    }

    var roleCodes = IdentityRoleCatalog.NormalizeAdministratorRoles(request.RoleCodes);
    var failures = ValidateAdministratorRequest(request.Email, request.DisplayName, request.RoleCodes).ToList();
    if (roleCodes.Length == 0)
    {
      failures.Add(new ValidationFailure(nameof(request.RoleCodes), "validation.role"));
    }

    if (failures.Count > 0)
    {
      return Invalid<AdministratorDetailDto>(failures);
    }

    var exists = await identityRepository.EmailExistsAsync(
        request.Email,
        UserAccountType.Admin,
        user.Id,
        cancellationToken)
      .ConfigureAwait(false);
    if (exists)
    {
      return Failed<AdministratorDetailDto>(IdentityOperationFailure.Conflict);
    }

    var now = timeProvider.GetUtcNow();
    user.ChangeProfile(request.Email, request.DisplayName, now, context.UserId);
    membership.AssignAccess(roleCodes, membership.PermissionCodes, now, context.UserId);

    await identityRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
    await identityRepository.UpdateMembershipAsync(membership, cancellationToken).ConfigureAwait(false);
    await sessionInvalidator.RevokeUserSessionsAsync(user.Id, now, "administrator-role-changed", cancellationToken)
      .ConfigureAwait(false);
    await WriteSecuritySideEffectsAsync("administrators.role.changed", context, user, cancellationToken)
      .ConfigureAwait(false);

    var detail = await identityRepository.GetAdministratorAsync(user.Id, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    return IdentityOperationResult<AdministratorDetailDto>.Success(detail!);
  }

  public Task<IdentityOperationResult<AdministratorDetailDto>> DeactivateAsync(
    Guid id,
    CancellationToken cancellationToken = default) =>
    ChangeStatusAsync(
      id,
      (user, now, actorId) => user.Deactivate(now, actorId),
      "administrators.deactivated",
      "administrator-deactivated",
      cancellationToken);

  public Task<IdentityOperationResult<AdministratorDetailDto>> ReactivateAsync(
    Guid id,
    CancellationToken cancellationToken = default) =>
    ChangeStatusAsync(
      id,
      (user, now, actorId) => user.Reactivate(now, actorId),
      "administrators.reactivated",
      "administrator-reactivated",
      cancellationToken);

  public async Task<IdentityOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    if (id == Guid.Empty)
    {
      return IdentityOperationResult.Failed(
        IdentityOperationFailure.Validation,
        ValidationResult.Invalid([new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId)]).ToErrorDictionary());
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Archive(PermissionModules.Administrators),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return IdentityOperationResult.Failed(IdentityOperationFailure.Forbidden);
    }

    var userId = new UserId(id);
    var user = await identityRepository.FindUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
    var membership = await identityRepository.FindMembershipAsync(userId, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (user is null || user.AccountType != UserAccountType.Admin || membership is null)
    {
      return IdentityOperationResult.Failed(IdentityOperationFailure.NotFound);
    }

    var now = timeProvider.GetUtcNow();
    user.Archive(now, context.UserId);
    membership.Archive(now, context.UserId);

    await identityRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
    await identityRepository.UpdateMembershipAsync(membership, cancellationToken).ConfigureAwait(false);
    await sessionInvalidator.RevokeUserSessionsAsync(user.Id, now, "administrator-archived", cancellationToken)
      .ConfigureAwait(false);
    await WriteSecuritySideEffectsAsync("administrators.archived", context, user, cancellationToken)
      .ConfigureAwait(false);

    return IdentityOperationResult.Success();
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetRoleOptionsAsync(
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(IdentityRoleCatalog.GetAdministratorRoleOptions());
  }

  private async Task<IdentityOperationResult<AdministratorDetailDto>> ChangeStatusAsync(
    Guid id,
    Action<IdentityUser, DateTimeOffset, UserId?> change,
    string auditAction,
    string revokeReason,
    CancellationToken cancellationToken)
  {
    if (id == Guid.Empty)
    {
      return Invalid<AdministratorDetailDto>([new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId)]);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Manage(PermissionModules.Administrators),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return Failed<AdministratorDetailDto>(IdentityOperationFailure.Forbidden);
    }

    var userId = new UserId(id);
    var user = await identityRepository.FindUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
    var membership = await identityRepository.FindMembershipAsync(userId, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (user is null || user.AccountType != UserAccountType.Admin || membership is null)
    {
      return Failed<AdministratorDetailDto>(IdentityOperationFailure.NotFound);
    }

    var now = timeProvider.GetUtcNow();
    change(user, now, context.UserId);
    await identityRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
    await sessionInvalidator.RevokeUserSessionsAsync(user.Id, now, revokeReason, cancellationToken)
      .ConfigureAwait(false);
    await WriteSecuritySideEffectsAsync(auditAction, context, user, cancellationToken).ConfigureAwait(false);

    var detail = await identityRepository.GetAdministratorAsync(user.Id, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    return IdentityOperationResult<AdministratorDetailDto>.Success(detail!);
  }

  private async Task<ActiveOrganizationContext?> AuthorizeContextAsync(
    string permissionCode,
    CancellationToken cancellationToken)
  {
    var permission = await permissionService.AuthorizeAsync(permissionCode, cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return null;
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    return context.Succeeded ? context.Context : null;
  }

  private async Task WriteSecuritySideEffectsAsync(
    string action,
    ActiveOrganizationContext context,
    IdentityUser targetUser,
    CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var subject = EntityReference.FromGuid("identityUser", targetUser.Id.Value, targetUser.Email);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["status"] = targetUser.Status.ToString(),
      ["accountType"] = targetUser.AccountType.ToString()
    };
    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "administrators",
      action,
      now,
      actor,
      subject,
      ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications,
      data);

    await auditWriter.WriteAsync(
        new AuditEntryDraft(
          context.OrganizationId,
          action,
          AuditEntryCategory.Security,
          actor,
          subject,
          now,
          data),
        cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static IEnumerable<ValidationFailure> ValidateAdministratorRequest(
    string? email,
    string? displayName,
    IReadOnlyList<string>? roleCodes)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      yield return new ValidationFailure(nameof(email), ValidationMessageKeys.Required);
    }
    else if (!email.Contains('@', StringComparison.Ordinal))
    {
      yield return new ValidationFailure(nameof(email), ValidationMessageKeys.Email);
    }

    if (string.IsNullOrWhiteSpace(displayName))
    {
      yield return new ValidationFailure(nameof(displayName), ValidationMessageKeys.Required);
    }

    if (roleCodes is null || roleCodes.Count == 0)
    {
      yield return new ValidationFailure(nameof(roleCodes), ValidationMessageKeys.Required);
    }
  }

  private static IEnumerable<ValidationFailure> ValidatePassword(string password) =>
    PasswordPolicy.Default.Validate(password)
      .FailureCodes
      .Select(code => new ValidationFailure(nameof(AdministratorCreateRequestDto.TemporaryPassword), code));

  private static IdentityOperationResult<T> Failed<T>(IdentityOperationFailure failure) =>
    IdentityOperationResult<T>.Failed(failure);

  private static IdentityOperationResult<T> Invalid<T>(IEnumerable<ValidationFailure> failures) =>
    IdentityOperationResult<T>.Failed(
      IdentityOperationFailure.Validation,
      ValidationResult.Invalid(failures).ToErrorDictionary());
}
