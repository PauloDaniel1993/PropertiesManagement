using Alsappan.Application.Common.Authorization;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Tests;

public sealed class AuthorizationContractTests
{
  [Fact]
  public void PermissionRequirementNormalizesPermissionCodes()
  {
    var organizationId = OrganizationId.New();
    var requirement = new PermissionRequirement(" Properties.Write ", organizationId);

    Assert.Equal(PermissionCodes.Write(PermissionModules.Properties), requirement.PermissionCode);
    Assert.Equal(organizationId, requirement.OrganizationId);
  }

  [Fact]
  public void PermissionEvaluationResultCarriesActiveOrganization()
  {
    var organizationId = OrganizationId.New();
    var requirement = new PermissionRequirement(PermissionCodes.Read(PermissionModules.Audit));

    var granted = PermissionEvaluationResult.Granted(requirement, organizationId);
    var denied = PermissionEvaluationResult.Denied(requirement, PermissionEvaluationFailure.PermissionDenied, organizationId);

    Assert.True(granted.IsGranted);
    Assert.Null(granted.Failure);
    Assert.Equal(organizationId, granted.ActiveOrganizationId);
    Assert.False(denied.IsGranted);
    Assert.Equal(PermissionEvaluationFailure.PermissionDenied, denied.Failure);
  }
}
