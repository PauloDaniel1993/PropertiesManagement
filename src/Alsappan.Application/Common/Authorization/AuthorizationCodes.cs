namespace Alsappan.Application.Common.Authorization;

public static class PermissionActions
{
  public const string Read = "read";
  public const string Write = "write";
  public const string Manage = "manage";
  public const string Archive = "archive";
  public const string Approve = "approve";
}

public static class PermissionModules
{
  public const string Administrators = "administrators";
  public const string Audit = "audit";
  public const string Branding = "branding";
  public const string Contracts = "contracts";
  public const string Dashboard = "dashboard";
  public const string Documents = "documents";
  public const string Identity = "identity";
  public const string Inspections = "inspections";
  public const string Notifications = "notifications";
  public const string Occurrences = "occurrences";
  public const string Payments = "payments";
  public const string Pets = "pets";
  public const string Properties = "properties";
  public const string Residents = "residents";
  public const string Settings = "settings";
  public const string Timeline = "timeline";
  public const string UtilityAccounts = "utility-accounts";
  public const string Vehicles = "vehicles";
}

public static class PermissionCodes
{
  public const string Wildcard = "*";
  public const string PlatformManage = "platform.manage";
  public const string OrganizationsSwitch = "organizations.switch";

  public static string Read(string module) => For(module, PermissionActions.Read);

  public static string Write(string module) => For(module, PermissionActions.Write);

  public static string Manage(string module) => For(module, PermissionActions.Manage);

  public static string Archive(string module) => For(module, PermissionActions.Archive);

  public static string Approve(string module) => For(module, PermissionActions.Approve);

  public static string For(string module, string action)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(module);
    ArgumentException.ThrowIfNullOrWhiteSpace(action);

    return $"{NormalizeSegment(module)}.{NormalizeSegment(action)}";
  }

  public static string Normalize(string permissionCode)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

#pragma warning disable CA1308
    var normalized = permissionCode.Trim().ToLowerInvariant();
#pragma warning restore CA1308
    return normalized == Wildcard ? Wildcard : normalized;
  }

#pragma warning disable CA1308
  private static string NormalizeSegment(string value) => value.Trim().ToLowerInvariant();
#pragma warning restore CA1308
}

public static class RoleCodes
{
  public const string PlatformOwner = "platform.owner";
  public const string OrganizationOwner = "organization.owner";
  public const string OrganizationAdmin = "organization.admin";
  public const string OrganizationManager = "organization.manager";
  public const string OrganizationStaff = "organization.staff";
  public const string OrganizationViewer = "organization.viewer";
  public const string ResidentUser = "resident.user";

  public static string Normalize(string roleCode)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
#pragma warning disable CA1308
    return roleCode.Trim().ToLowerInvariant();
#pragma warning restore CA1308
  }
}
