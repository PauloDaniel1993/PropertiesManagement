using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Authorization;

namespace Alsappan.Infrastructure.Tests;

public sealed class AuthorizationPermissionServiceTests
{
  [Fact]
  public async Task PermissionServiceGrantsRoleBackedPermissionForActiveOrganization()
  {
    var organizationId = OrganizationId.New();
    var context = BuildContext(
      organizationId,
      new OrganizationMembership(organizationId, [RoleCodes.OrganizationStaff]));
    var service = new DefaultPermissionService(
      new FixedActiveOrganizationContextResolver(context),
      new DefaultRolePermissionCatalog());

    var result = await service.AuthorizeAsync(PermissionCodes.Write(PermissionModules.Properties));

    Assert.True(result.IsGranted);
    Assert.Equal(organizationId, result.ActiveOrganizationId);
  }

  [Fact]
  public async Task PermissionServiceDeniesUnknownPermission()
  {
    var organizationId = OrganizationId.New();
    var context = BuildContext(
      organizationId,
      new OrganizationMembership(organizationId, [RoleCodes.OrganizationViewer]));
    var service = new DefaultPermissionService(
      new FixedActiveOrganizationContextResolver(context),
      new DefaultRolePermissionCatalog());

    var result = await service.AuthorizeAsync(PermissionCodes.Write(PermissionModules.Settings));

    Assert.False(result.IsGranted);
    Assert.Equal(PermissionEvaluationFailure.PermissionDenied, result.Failure);
  }

  [Fact]
  public async Task PermissionServiceDeniesOrganizationMismatch()
  {
    var activeOrganizationId = OrganizationId.New();
    var requestedOrganizationId = OrganizationId.New();
    var context = BuildContext(
      activeOrganizationId,
      new OrganizationMembership(activeOrganizationId, permissionCodes: [PermissionCodes.Wildcard]));
    var service = new DefaultPermissionService(
      new FixedActiveOrganizationContextResolver(context),
      new DefaultRolePermissionCatalog());

    var result = await service.AuthorizeAsync(
      new PermissionRequirement(PermissionCodes.Read(PermissionModules.Properties), requestedOrganizationId));

    Assert.False(result.IsGranted);
    Assert.Equal(PermissionEvaluationFailure.OrganizationMismatch, result.Failure);
  }

  [Fact]
  public void ResidentUserRoleDoesNotGrantAdminPetOrVehicleModulePermissions()
  {
    var catalog = new DefaultRolePermissionCatalog();
    var permissions = catalog.GetPermissionsForRoles([RoleCodes.ResidentUser]);

    Assert.DoesNotContain(PermissionCodes.Read(PermissionModules.Pets), permissions);
    Assert.DoesNotContain(PermissionCodes.Write(PermissionModules.Pets), permissions);
    Assert.DoesNotContain(PermissionCodes.Manage(PermissionModules.Pets), permissions);
    Assert.DoesNotContain(PermissionCodes.Read(PermissionModules.Vehicles), permissions);
    Assert.DoesNotContain(PermissionCodes.Write(PermissionModules.Vehicles), permissions);
    Assert.DoesNotContain(PermissionCodes.Manage(PermissionModules.Vehicles), permissions);
  }

  private static ActiveOrganizationContext BuildContext(
    OrganizationId organizationId,
    OrganizationMembership membership)
  {
    var user = new AuthenticatedUser(UserId.New(), memberships: [membership]);
    Assert.Equal(organizationId, membership.OrganizationId);
    return new ActiveOrganizationContext(user, membership);
  }

  private sealed class FixedActiveOrganizationContextResolver : IActiveOrganizationContextResolver
  {
    private readonly ActiveOrganizationContext context;

    public FixedActiveOrganizationContextResolver(ActiveOrganizationContext context)
    {
      this.context = context;
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(CancellationToken cancellationToken = default) =>
      ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
  }
}
