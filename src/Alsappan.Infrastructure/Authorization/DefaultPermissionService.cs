using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Tenancy;

namespace Alsappan.Infrastructure.Authorization;

public sealed class DefaultPermissionService : IPermissionService
{
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IRolePermissionCatalog rolePermissionCatalog;

  public DefaultPermissionService(
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IRolePermissionCatalog rolePermissionCatalog)
  {
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.rolePermissionCatalog = rolePermissionCatalog ?? throw new ArgumentNullException(nameof(rolePermissionCatalog));
  }

  public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
    string permissionCode,
    CancellationToken cancellationToken = default) =>
    AuthorizeAsync(new PermissionRequirement(permissionCode), cancellationToken);

  public async ValueTask<PermissionEvaluationResult> AuthorizeAsync(
    PermissionRequirement requirement,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(requirement);

    var resolution = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    if (!resolution.Succeeded)
    {
      return PermissionEvaluationResult.Denied(
        requirement,
        MapFailure(resolution.Failure));
    }

    var context = resolution.Context!;
    if (requirement.OrganizationId.HasValue && requirement.OrganizationId.Value != context.OrganizationId)
    {
      return PermissionEvaluationResult.Denied(
        requirement,
        PermissionEvaluationFailure.OrganizationMismatch,
        context.OrganizationId);
    }

    var effectivePermissions = BuildEffectivePermissions(context);
    if (HasPermission(effectivePermissions, requirement.PermissionCode))
    {
      return PermissionEvaluationResult.Granted(requirement, context.OrganizationId);
    }

    return PermissionEvaluationResult.Denied(
      requirement,
      PermissionEvaluationFailure.PermissionDenied,
      context.OrganizationId);
  }

  public async ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(
    CancellationToken cancellationToken = default)
  {
    var resolution = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    return resolution.Succeeded
      ? BuildEffectivePermissions(resolution.Context!)
      : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  }

  private HashSet<string> BuildEffectivePermissions(ActiveOrganizationContext context)
  {
    var permissions = new HashSet<string>(context.Membership.PermissionCodes, StringComparer.OrdinalIgnoreCase);

    permissions.UnionWith(rolePermissionCatalog.GetPermissionsForRoles(context.Membership.RoleCodes));
    permissions.UnionWith(context.User.PlatformPermissionCodes);
    permissions.UnionWith(rolePermissionCatalog.GetPermissionsForRoles(context.User.PlatformRoleCodes));

    return permissions;
  }

  private static bool HasPermission(HashSet<string> permissions, string permissionCode) =>
    permissions.Contains(PermissionCodes.Wildcard) || permissions.Contains(PermissionCodes.Normalize(permissionCode));

  private static PermissionEvaluationFailure MapFailure(ActiveOrganizationResolutionFailure? failure) =>
    failure == ActiveOrganizationResolutionFailure.Unauthenticated
      ? PermissionEvaluationFailure.Unauthenticated
      : PermissionEvaluationFailure.NoActiveOrganization;
}
