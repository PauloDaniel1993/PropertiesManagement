using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Identity;

namespace Alsappan.Application.Tests.Identity;

public sealed class IdentityRoleCatalogTests
{
  [Fact]
  public void AdministratorRoleOptionsUsePortugueseLabelsByDefault()
  {
    var options = IdentityRoleCatalog.GetAdministratorRoleOptions();

    Assert.Contains(options, option =>
      option.Value == RoleCodes.OrganizationAdmin &&
      option.Label == "Administrador");
    Assert.DoesNotContain(options, option => option.Value == RoleCodes.ResidentUser);
  }

  [Fact]
  public void AdministratorRolesRejectResidentRole()
  {
    var roles = IdentityRoleCatalog.NormalizeAdministratorRoles(
      [RoleCodes.OrganizationAdmin, RoleCodes.ResidentUser]);

    Assert.Empty(roles);
  }
}
