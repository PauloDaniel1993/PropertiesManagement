using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Identity;

public static class IdentityRoleCatalog
{
  private static readonly RoleOption[] StarterRoles =
  [
    new(RoleCodes.OrganizationAdmin, "Administrador", "Administrator", true),
    new(RoleCodes.OrganizationManager, "Gestor", "Manager", true),
    new(RoleCodes.OrganizationStaff, "Operador", "Operator", true),
    new(RoleCodes.OrganizationViewer, "Leitura", "Read only", true),
    new(RoleCodes.ResidentUser, "Morador", "Resident", false)
  ];

  public static IReadOnlyList<SelectOptionDto> GetAdministratorRoleOptions(string? locale = null) =>
    StarterRoles
      .Where(role => role.IsAdministratorRole)
      .Select(role => role.ToOption(locale))
      .ToArray();

  public static IReadOnlyList<SelectOptionDto> GetStarterRoleOptions(string? locale = null) =>
    StarterRoles
      .Select(role => role.ToOption(locale))
      .ToArray();

  public static bool IsAdministratorRole(string roleCode) =>
    StarterRoles.Any(role =>
      role.IsAdministratorRole &&
      string.Equals(role.Code, RoleCodes.Normalize(roleCode), StringComparison.OrdinalIgnoreCase));

  public static string[] NormalizeAdministratorRoles(IEnumerable<string>? roleCodes)
  {
    var normalized = (roleCodes ?? [])
      .Where(role => !string.IsNullOrWhiteSpace(role))
      .Select(RoleCodes.Normalize)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();

    return normalized.Length == 0 || normalized.Any(role => !IsAdministratorRole(role))
      ? []
      : normalized;
  }

  private sealed record RoleOption(
    string Code,
    string PtBrLabel,
    string EnUsLabel,
    bool IsAdministratorRole)
  {
    public SelectOptionDto ToOption(string? locale)
    {
      var label = string.Equals(locale, "en-US", StringComparison.OrdinalIgnoreCase)
        ? EnUsLabel
        : PtBrLabel;

      return new SelectOptionDto(Code, label);
    }
  }
}
