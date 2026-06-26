using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Tenancy;

namespace Alsappan.Infrastructure.Tests;

public sealed class TenancyContextResolverTests
{
  [Fact]
  public async Task ResolverUsesRequestedOrganizationWhenUserIsMember()
  {
    var requestedOrganizationId = OrganizationId.New();
    var otherOrganizationId = OrganizationId.New();
    var user = new AuthenticatedUser(
      UserId.New(),
      memberships:
      [
        new OrganizationMembership(otherOrganizationId, [RoleCodes.OrganizationViewer]),
        new OrganizationMembership(requestedOrganizationId, [RoleCodes.OrganizationAdmin])
      ]);
    var resolver = new ClaimsActiveOrganizationContextResolver(
      new FixedAuthenticatedUserProvider(user),
      new FixedSelectionProvider(requestedOrganizationId.ToString()));

    var result = await resolver.ResolveAsync();

    Assert.True(result.Succeeded);
    Assert.Equal(requestedOrganizationId, result.Context!.OrganizationId);
  }

  [Fact]
  public async Task ResolverRejectsRequestedOrganizationOutsideMemberships()
  {
    var user = new AuthenticatedUser(
      UserId.New(),
      memberships: [new OrganizationMembership(OrganizationId.New())]);
    var resolver = new ClaimsActiveOrganizationContextResolver(
      new FixedAuthenticatedUserProvider(user),
      new FixedSelectionProvider(OrganizationId.New().ToString()));

    var result = await resolver.ResolveAsync();

    Assert.False(result.Succeeded);
    Assert.Equal(ActiveOrganizationResolutionFailure.RequestedOrganizationNotInMemberships, result.Failure);
  }

  [Fact]
  public async Task ResolverRequiresSelectionForMultipleMembershipsWithoutActiveMarker()
  {
    var user = new AuthenticatedUser(
      UserId.New(),
      memberships:
      [
        new OrganizationMembership(OrganizationId.New()),
        new OrganizationMembership(OrganizationId.New())
      ]);
    var resolver = new ClaimsActiveOrganizationContextResolver(new FixedAuthenticatedUserProvider(user));

    var result = await resolver.ResolveAsync();

    Assert.False(result.Succeeded);
    Assert.Equal(ActiveOrganizationResolutionFailure.MultipleMembershipsRequireSelection, result.Failure);
  }

  private sealed class FixedAuthenticatedUserProvider : IAuthenticatedUserProvider
  {
    private readonly AuthenticatedUser? user;

    public FixedAuthenticatedUserProvider(AuthenticatedUser? user)
    {
      this.user = user;
    }

    public ValueTask<AuthenticatedUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
      ValueTask.FromResult(user);
  }

  private sealed class FixedSelectionProvider : IActiveOrganizationSelectionProvider
  {
    private readonly string? organizationId;

    public FixedSelectionProvider(string? organizationId)
    {
      this.organizationId = organizationId;
    }

    public string? GetRequestedOrganizationId() => organizationId;
  }
}
