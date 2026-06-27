using Alsappan.Application.Common.Authorization;

namespace Alsappan.Infrastructure.Authorization;

public sealed class DefaultRolePermissionCatalog : IRolePermissionCatalog
{
  private static readonly string[] AllOrganizationModules =
  [
    PermissionModules.Administrators,
    PermissionModules.Audit,
    PermissionModules.Branding,
    PermissionModules.Contracts,
    PermissionModules.Dashboard,
    PermissionModules.Documents,
    PermissionModules.Identity,
    PermissionModules.Inspections,
    PermissionModules.Notifications,
    PermissionModules.Occurrences,
    PermissionModules.Payments,
    PermissionModules.Pets,
    PermissionModules.Properties,
    PermissionModules.Residents,
    PermissionModules.Settings,
    PermissionModules.Timeline,
    PermissionModules.UtilityAccounts,
    PermissionModules.Vehicles
  ];

  private static readonly string[] OperationalModules =
  [
    PermissionModules.Contracts,
    PermissionModules.Documents,
    PermissionModules.Inspections,
    PermissionModules.Notifications,
    PermissionModules.Occurrences,
    PermissionModules.Payments,
    PermissionModules.Pets,
    PermissionModules.Properties,
    PermissionModules.Residents,
    PermissionModules.Timeline,
    PermissionModules.UtilityAccounts,
    PermissionModules.Vehicles
  ];

  private static readonly Dictionary<string, string[]> RolePermissions =
    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
      [RoleCodes.PlatformOwner] = [PermissionCodes.Wildcard],
      [RoleCodes.OrganizationOwner] = [PermissionCodes.Wildcard],
      [RoleCodes.OrganizationAdmin] = [PermissionCodes.Wildcard],
      [RoleCodes.OrganizationManager] =
      [
        .. ForModules(AllOrganizationModules, PermissionActions.Read),
        .. ForModules(OperationalModules, PermissionActions.Write, PermissionActions.Archive),
        PermissionCodes.Manage(PermissionModules.Notifications),
        PermissionCodes.Manage(PermissionModules.Settings)
      ],
      [RoleCodes.OrganizationStaff] =
      [
        .. ForModules(OperationalModules, PermissionActions.Read, PermissionActions.Write),
        PermissionCodes.Read(PermissionModules.Dashboard)
      ],
      [RoleCodes.OrganizationViewer] =
      [
        .. ForModules(AllOrganizationModules, PermissionActions.Read)
      ],
      [RoleCodes.ResidentUser] =
      [
        PermissionCodes.Read(PermissionModules.Dashboard),
        PermissionCodes.Read(PermissionModules.Documents),
        PermissionCodes.Read(PermissionModules.Notifications),
        PermissionCodes.Read(PermissionModules.Occurrences),
        PermissionCodes.Write(PermissionModules.Occurrences),
        PermissionCodes.Read(PermissionModules.Payments)
      ]
    };

  public IReadOnlySet<string> GetPermissionsForRoles(IEnumerable<string> roleCodes)
  {
    var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var roleCode in roleCodes ?? [])
    {
      if (string.IsNullOrWhiteSpace(roleCode))
      {
        continue;
      }

      if (!RolePermissions.TryGetValue(RoleCodes.Normalize(roleCode), out var rolePermissions))
      {
        continue;
      }

      foreach (var permission in rolePermissions)
      {
        permissions.Add(PermissionCodes.Normalize(permission));
      }
    }

    return permissions;
  }

  public bool IsKnownRole(string roleCode)
  {
    if (string.IsNullOrWhiteSpace(roleCode))
    {
      return false;
    }

    return RolePermissions.ContainsKey(RoleCodes.Normalize(roleCode));
  }

  private static string[] ForModules(IEnumerable<string> modules, params string[] actions) =>
    modules
      .SelectMany(module => actions.Select(action => PermissionCodes.For(module, action)))
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();
}
