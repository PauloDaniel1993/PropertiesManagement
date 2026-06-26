using Alsappan.Application.Common.Auth;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Tenancy;

public sealed record ActiveOrganizationContext
{
  public ActiveOrganizationContext(AuthenticatedUser user, OrganizationMembership membership)
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(membership);

    if (!user.HasMembership(membership.OrganizationId))
    {
      throw new ArgumentException("Active organization membership must belong to the authenticated user.", nameof(membership));
    }

    User = user;
    Membership = membership;
  }

  public AuthenticatedUser User { get; }

  public OrganizationMembership Membership { get; }

  public UserId UserId => User.UserId;

  public OrganizationId OrganizationId => Membership.OrganizationId;
}
