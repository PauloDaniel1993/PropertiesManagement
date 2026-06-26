using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Auth;

public sealed record OrganizationMembershipClaimDto(
  Guid OrganizationId,
  IReadOnlyList<string> RoleCodes,
  IReadOnlyList<string> PermissionCodes,
  bool IsActive)
{
  public static OrganizationMembershipClaimDto FromMembership(OrganizationMembership membership)
  {
    ArgumentNullException.ThrowIfNull(membership);

    return new OrganizationMembershipClaimDto(
      membership.OrganizationId.Value,
      membership.RoleCodes.ToArray(),
      membership.PermissionCodes.ToArray(),
      membership.IsActive);
  }

  public OrganizationMembership ToMembership() =>
    new(
      new OrganizationId(OrganizationId),
      RoleCodes,
      PermissionCodes,
      IsActive);
}
