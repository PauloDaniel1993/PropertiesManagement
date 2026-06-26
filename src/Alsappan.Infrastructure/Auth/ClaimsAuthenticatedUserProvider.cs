using System.Security.Claims;
using System.Text.Json;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Infrastructure.Auth;

public sealed class ClaimsAuthenticatedUserProvider : IAuthenticatedUserProvider
{
  private static readonly JsonSerializerOptions ClaimJsonOptions = new(JsonSerializerDefaults.Web);

  private readonly IClaimsPrincipalAccessor claimsPrincipalAccessor;

  public ClaimsAuthenticatedUserProvider(IClaimsPrincipalAccessor claimsPrincipalAccessor)
  {
    this.claimsPrincipalAccessor = claimsPrincipalAccessor ?? throw new ArgumentNullException(nameof(claimsPrincipalAccessor));
  }

  public ValueTask<AuthenticatedUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var principal = claimsPrincipalAccessor.Principal;
    if (principal?.Identity?.IsAuthenticated != true)
    {
      return ValueTask.FromResult<AuthenticatedUser?>(null);
    }

    var userIdValue = FindFirstValue(principal, AuthClaimTypes.UserId, ClaimTypes.NameIdentifier);
    if (!TryParseGuid(userIdValue, out var userId))
    {
      return ValueTask.FromResult<AuthenticatedUser?>(null);
    }

    var memberships = ParseMembershipClaims(principal).ToArray();
    if (memberships.Length == 0)
    {
      memberships = BuildFallbackActiveMembership(principal);
    }

    var user = new AuthenticatedUser(
      new UserId(userId),
      FindFirstValue(principal, AuthClaimTypes.Email, ClaimTypes.Email),
      FindFirstValue(principal, AuthClaimTypes.DisplayName, ClaimTypes.Name),
      memberships,
      ClaimValues(principal, AuthClaimTypes.PlatformRole),
      ClaimValues(principal, AuthClaimTypes.PlatformPermission));

    return ValueTask.FromResult<AuthenticatedUser?>(user);
  }

  private static IEnumerable<OrganizationMembership> ParseMembershipClaims(ClaimsPrincipal principal)
  {
    foreach (var claim in principal.FindAll(AuthClaimTypes.Membership))
    {
      OrganizationMembershipClaimDto? membershipClaim;

      try
      {
        membershipClaim = JsonSerializer.Deserialize<OrganizationMembershipClaimDto>(claim.Value, ClaimJsonOptions);
      }
      catch (JsonException)
      {
        continue;
      }
      catch (ArgumentException)
      {
        continue;
      }

      if (membershipClaim is null || membershipClaim.OrganizationId == Guid.Empty)
      {
        continue;
      }

      yield return membershipClaim.ToMembership();
    }
  }

  private static OrganizationMembership[] BuildFallbackActiveMembership(ClaimsPrincipal principal)
  {
    var organizationIdValue = FindFirstValue(principal, AuthClaimTypes.ActiveOrganizationId);
    if (!TryParseGuid(organizationIdValue, out var organizationId))
    {
      return [];
    }

    return
    [
      new OrganizationMembership(
        new OrganizationId(organizationId),
        ClaimValues(principal, AuthClaimTypes.OrganizationRole, ClaimTypes.Role, "role"),
        ClaimValues(principal, AuthClaimTypes.OrganizationPermission),
        isActive: true)
    ];
  }

  private static IEnumerable<string> ClaimValues(ClaimsPrincipal principal, params string[] claimTypes) =>
    claimTypes
      .SelectMany(claimType => principal.FindAll(claimType))
      .Select(claim => claim.Value)
      .Where(value => !string.IsNullOrWhiteSpace(value));

  private static string? FindFirstValue(ClaimsPrincipal principal, params string[] claimTypes)
  {
    foreach (var claimType in claimTypes)
    {
      var value = principal.FindFirst(claimType)?.Value;
      if (!string.IsNullOrWhiteSpace(value))
      {
        return value;
      }
    }

    return null;
  }

  private static bool TryParseGuid(string? value, out Guid id)
  {
    if (Guid.TryParse(value, out id) && id != Guid.Empty)
    {
      return true;
    }

    id = Guid.Empty;
    return false;
  }
}
