using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Auth;

namespace Alsappan.Infrastructure.Tests;

public sealed class AuthTokenServiceTests
{
  private static readonly JsonSerializerOptions ClaimJsonOptions = new(JsonSerializerDefaults.Web);

  [Fact]
  public void JwtAccessTokenServiceIssuesMembershipClaims()
  {
    var userId = UserId.New();
    var organizationId = OrganizationId.New();
    var sessionId = EntityId.New();
    var service = new JwtAccessTokenService(new AuthOptions
    {
      Issuer = "Alsappan.Tests",
      Audience = "Alsappan.Web.Tests",
      SigningKey = "0123456789abcdef0123456789abcdef",
      AccessTokenMinutes = 20
    });

    var issued = service.IssueToken(new AccessTokenDescriptor(
      userId,
      [
        new OrganizationMembership(
          organizationId,
          [RoleCodes.OrganizationAdmin],
          [PermissionCodes.Read(PermissionModules.Properties)],
          isActive: true)
      ],
      email: "admin@example.com",
      displayName: "Admin",
      refreshSessionId: sessionId));

    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);

    Assert.Equal(AuthTransportConstants.BearerScheme, issued.TokenType);
    Assert.Equal("Alsappan.Tests", jwt.Issuer);
    Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == userId.ToString());
    Assert.Contains(jwt.Claims, claim => claim.Type == AuthClaimTypes.SessionId && claim.Value == sessionId.ToString());
    Assert.Contains(jwt.Claims, claim => claim.Type == AuthClaimTypes.ActiveOrganizationId && claim.Value == organizationId.ToString());
    Assert.Contains(jwt.Claims, claim => claim.Type == AuthClaimTypes.Membership);
  }

  [Fact]
  public async Task ClaimsAuthenticatedUserProviderParsesMembershipClaims()
  {
    var userId = UserId.New();
    var organizationId = OrganizationId.New();
    var membership = new OrganizationMembership(
      organizationId,
      [" organization.viewer "],
      [" dashboard.read "],
      isActive: true);
    var membershipClaim = JsonSerializer.Serialize(
      OrganizationMembershipClaimDto.FromMembership(membership),
      ClaimJsonOptions);
    var principal = new ClaimsPrincipal(new ClaimsIdentity(
    [
      new Claim(AuthClaimTypes.UserId, userId.ToString()),
      new Claim(AuthClaimTypes.Email, "viewer@example.com"),
      new Claim(AuthClaimTypes.Membership, membershipClaim)
    ], "Test"));
    var provider = new ClaimsAuthenticatedUserProvider(new FixedClaimsPrincipalAccessor(principal));

    var user = await provider.GetCurrentUserAsync();

    Assert.NotNull(user);
    Assert.Equal(userId, user.UserId);
    Assert.Equal("viewer@example.com", user.Email);
    Assert.True(user.HasMembership(organizationId));
    Assert.Contains(RoleCodes.OrganizationViewer, user.Memberships[0].RoleCodes);
  }

  [Fact]
  public void Sha256RefreshTokenProtectorCreatesOpaqueTokenAndStableHash()
  {
    var protector = new Sha256RefreshTokenProtector();

    var secret = protector.CreateToken();

    Assert.NotEqual(secret.Token, secret.TokenHash);
    Assert.True(protector.TokenMatchesHash(secret.Token, secret.TokenHash));
    Assert.False(protector.TokenMatchesHash($"{secret.Token}x", secret.TokenHash));
  }

  private sealed class FixedClaimsPrincipalAccessor : IClaimsPrincipalAccessor
  {
    public FixedClaimsPrincipalAccessor(ClaimsPrincipal principal)
    {
      Principal = principal;
    }

    public ClaimsPrincipal? Principal { get; }
  }
}
