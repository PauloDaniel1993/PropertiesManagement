using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Infrastructure.Tenancy;

public sealed class ClaimsActiveOrganizationContextResolver : IActiveOrganizationContextResolver
{
  private readonly IAuthenticatedUserProvider authenticatedUserProvider;
  private readonly IActiveOrganizationSelectionProvider? selectionProvider;

  public ClaimsActiveOrganizationContextResolver(IAuthenticatedUserProvider authenticatedUserProvider)
    : this(authenticatedUserProvider, null)
  {
  }

  public ClaimsActiveOrganizationContextResolver(
    IAuthenticatedUserProvider authenticatedUserProvider,
    IActiveOrganizationSelectionProvider? selectionProvider)
  {
    this.authenticatedUserProvider = authenticatedUserProvider ?? throw new ArgumentNullException(nameof(authenticatedUserProvider));
    this.selectionProvider = selectionProvider;
  }

  public async ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(
    CancellationToken cancellationToken = default)
  {
    var user = await authenticatedUserProvider.GetCurrentUserAsync(cancellationToken)
      .ConfigureAwait(false);
    if (user is null)
    {
      return ActiveOrganizationResolutionResult.Denied(ActiveOrganizationResolutionFailure.Unauthenticated);
    }

    if (user.Memberships.Count == 0)
    {
      return ActiveOrganizationResolutionResult.Denied(ActiveOrganizationResolutionFailure.NoMemberships);
    }

    var requestedOrganizationId = selectionProvider?.GetRequestedOrganizationId();
    if (!string.IsNullOrWhiteSpace(requestedOrganizationId))
    {
      return ResolveRequestedOrganization(user, requestedOrganizationId);
    }

    var activeMemberships = user.Memberships.Where(membership => membership.IsActive).ToArray();
    if (activeMemberships.Length == 1)
    {
      return Success(user, activeMemberships[0]);
    }

    if (activeMemberships.Length > 1)
    {
      return ActiveOrganizationResolutionResult.Denied(ActiveOrganizationResolutionFailure.MultipleActiveMemberships);
    }

    if (user.Memberships.Count == 1)
    {
      return Success(user, user.Memberships[0]);
    }

    return ActiveOrganizationResolutionResult.Denied(ActiveOrganizationResolutionFailure.MultipleMembershipsRequireSelection);
  }

  private static ActiveOrganizationResolutionResult ResolveRequestedOrganization(
    AuthenticatedUser user,
    string requestedOrganizationId)
  {
    if (!Guid.TryParse(requestedOrganizationId, out var organizationIdValue) || organizationIdValue == Guid.Empty)
    {
      return ActiveOrganizationResolutionResult.Denied(ActiveOrganizationResolutionFailure.InvalidRequestedOrganization);
    }

    var organizationId = new OrganizationId(organizationIdValue);
    var membership = user.FindMembership(organizationId);
    if (membership is null)
    {
      return ActiveOrganizationResolutionResult.Denied(ActiveOrganizationResolutionFailure.RequestedOrganizationNotInMemberships);
    }

    return Success(user, membership);
  }

  private static ActiveOrganizationResolutionResult Success(
    AuthenticatedUser user,
    OrganizationMembership membership) =>
    ActiveOrganizationResolutionResult.Success(new ActiveOrganizationContext(user, membership));
}
