using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Tests;

public sealed class TenancyContractTests
{
  [Fact]
  public void ActiveOrganizationContextRequiresMembershipBelongingToUser()
  {
    var userOrganizationId = OrganizationId.New();
    var otherOrganizationId = OrganizationId.New();
    var user = new AuthenticatedUser(
      UserId.New(),
      memberships: [new OrganizationMembership(userOrganizationId)]);

    Assert.Throws<ArgumentException>(() =>
      new ActiveOrganizationContext(user, new OrganizationMembership(otherOrganizationId)));
  }

  [Fact]
  public void OrganizationMembershipNormalizesRoleAndPermissionCodes()
  {
    var membership = new OrganizationMembership(
      OrganizationId.New(),
      [" Organization.Manager ", "organization.manager"],
      [" Payments.Read ", "payments.read"],
      isActive: true);

    Assert.True(membership.IsActive);
    Assert.Single(membership.RoleCodes);
    Assert.Single(membership.PermissionCodes);
    Assert.Contains("organization.manager", membership.RoleCodes);
    Assert.Contains("payments.read", membership.PermissionCodes);
  }
}
