namespace Alsappan.Application.Common.Authorization;

public interface IRolePermissionCatalog
{
  IReadOnlySet<string> GetPermissionsForRoles(IEnumerable<string> roleCodes);

  bool IsKnownRole(string roleCode);
}
