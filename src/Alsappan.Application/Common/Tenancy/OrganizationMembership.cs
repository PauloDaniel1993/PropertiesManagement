using Alsappan.Application.Common.Authorization;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Tenancy;

public sealed class OrganizationMembership
{
  public OrganizationMembership(
    OrganizationId organizationId,
    IEnumerable<string>? roleCodes = null,
    IEnumerable<string>? permissionCodes = null,
    bool isActive = false)
  {
    if (organizationId.Value == Guid.Empty)
    {
      throw new ArgumentException("Organization id is required.", nameof(organizationId));
    }

    OrganizationId = organizationId;
    RoleCodes = NormalizeCodes(roleCodes, Authorization.RoleCodes.Normalize);
    PermissionCodes = NormalizeCodes(permissionCodes, Authorization.PermissionCodes.Normalize);
    IsActive = isActive;
  }

  public OrganizationId OrganizationId { get; }

  public IReadOnlySet<string> RoleCodes { get; }

  public IReadOnlySet<string> PermissionCodes { get; }

  public bool IsActive { get; }

  private static HashSet<string> NormalizeCodes(
    IEnumerable<string>? values,
    Func<string, string> normalize)
  {
    var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var value in values ?? [])
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        continue;
      }

      codes.Add(normalize(value));
    }

    return codes;
  }
}
