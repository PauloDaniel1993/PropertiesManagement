using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Infrastructure.Auth;

public sealed class JwtAccessTokenService : IAccessTokenService
{
  private static readonly JsonSerializerOptions ClaimJsonOptions = new(JsonSerializerDefaults.Web);

  private readonly AuthOptions authOptions;
  private readonly TimeProvider timeProvider;
  private readonly JwtSecurityTokenHandler tokenHandler = new();

  public JwtAccessTokenService(AuthOptions authOptions, TimeProvider? timeProvider = null)
  {
    this.authOptions = authOptions ?? throw new ArgumentNullException(nameof(authOptions));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public IssuedAccessToken IssueToken(AccessTokenDescriptor descriptor)
  {
    ArgumentNullException.ThrowIfNull(descriptor);

    var signingKey = CreateSigningKey();
    var issuedAt = timeProvider.GetUtcNow();
    var expiresAt = issuedAt.AddMinutes(GetAccessTokenMinutes());

    var token = new JwtSecurityToken(
      issuer: authOptions.Issuer,
      audience: authOptions.Audience,
      claims: BuildClaims(descriptor),
      notBefore: issuedAt.UtcDateTime,
      expires: expiresAt.UtcDateTime,
      signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

    return new IssuedAccessToken(tokenHandler.WriteToken(token), issuedAt, expiresAt);
  }

  private static IEnumerable<Claim> BuildClaims(AccessTokenDescriptor descriptor)
  {
    yield return new Claim(JwtRegisteredClaimNames.Sub, descriptor.UserId.ToString());
    yield return new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"));

    if (!string.IsNullOrWhiteSpace(descriptor.Email))
    {
      yield return new Claim(JwtRegisteredClaimNames.Email, descriptor.Email);
    }

    if (!string.IsNullOrWhiteSpace(descriptor.DisplayName))
    {
      yield return new Claim(JwtRegisteredClaimNames.Name, descriptor.DisplayName);
    }

    if (descriptor.RefreshSessionId.HasValue)
    {
      yield return new Claim(AuthClaimTypes.SessionId, descriptor.RefreshSessionId.Value.ToString());
    }

    var activeMembership = descriptor.Memberships.FirstOrDefault(membership => membership.IsActive)
      ?? (descriptor.Memberships.Count == 1 ? descriptor.Memberships[0] : null);

    if (activeMembership is not null)
    {
      yield return new Claim(AuthClaimTypes.ActiveOrganizationId, activeMembership.OrganizationId.ToString());
    }

    foreach (var membership in descriptor.Memberships)
    {
      var membershipClaim = OrganizationMembershipClaimDto.FromMembership(membership);
      yield return new Claim(AuthClaimTypes.Membership, JsonSerializer.Serialize(membershipClaim, ClaimJsonOptions));
    }

    foreach (var roleCode in descriptor.PlatformRoleCodes)
    {
      yield return new Claim(AuthClaimTypes.PlatformRole, roleCode);
    }

    foreach (var permissionCode in descriptor.PlatformPermissionCodes)
    {
      yield return new Claim(AuthClaimTypes.PlatformPermission, permissionCode);
    }
  }

  private SymmetricSecurityKey CreateSigningKey()
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(authOptions.SigningKey);

    var signingKeyBytes = Encoding.UTF8.GetBytes(authOptions.SigningKey);
    if (signingKeyBytes.Length < 32)
    {
      throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
    }

    return new SymmetricSecurityKey(signingKeyBytes);
  }

  private int GetAccessTokenMinutes()
  {
    if (authOptions.AccessTokenMinutes <= 0)
    {
      throw new InvalidOperationException("Access token lifetime must be greater than zero minutes.");
    }

    return authOptions.AccessTokenMinutes;
  }
}
