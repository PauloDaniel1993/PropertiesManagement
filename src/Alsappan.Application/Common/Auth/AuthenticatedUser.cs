using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Auth;

public sealed class AuthenticatedUser
{
  public AuthenticatedUser(
    UserId userId,
    string? email = null,
    string? displayName = null,
    IEnumerable<OrganizationMembership>? memberships = null,
    IEnumerable<string>? platformRoleCodes = null,
    IEnumerable<string>? platformPermissionCodes = null)
  {
    if (userId.Value == Guid.Empty)
    {
      throw new ArgumentException("User id is required.", nameof(userId));
    }

    UserId = userId;
    Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    Memberships = (memberships ?? [])
      .OrderBy(membership => membership.OrganizationId.Value)
      .ToArray();
    PlatformRoleCodes = NormalizeCodes(platformRoleCodes, RoleCodes.Normalize);
    PlatformPermissionCodes = NormalizeCodes(platformPermissionCodes, PermissionCodes.Normalize);
  }

  public UserId UserId { get; }

  public string? Email { get; }

  public string? DisplayName { get; }

  public IReadOnlyList<OrganizationMembership> Memberships { get; }

  public IReadOnlySet<string> PlatformRoleCodes { get; }

  public IReadOnlySet<string> PlatformPermissionCodes { get; }

  public OrganizationMembership? FindMembership(OrganizationId organizationId) =>
    Memberships.FirstOrDefault(membership => membership.OrganizationId == organizationId);

  public bool HasMembership(OrganizationId organizationId) => FindMembership(organizationId) is not null;

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
