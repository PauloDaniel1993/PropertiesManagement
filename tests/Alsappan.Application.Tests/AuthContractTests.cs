using System.Globalization;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Tests;

public sealed class AuthContractTests
{
  [Fact]
  public void AuthenticatedUserNormalizesOrganizationMembershipAndPlatformClaims()
  {
    var organizationId = OrganizationId.New();
    var user = new AuthenticatedUser(
      UserId.New(),
      email: "  admin@example.com ",
      displayName: "  Maria Silva ",
      memberships:
      [
        new OrganizationMembership(
          organizationId,
          [" Organization.Admin "],
          [" Properties.Read "],
          isActive: true)
      ],
      platformRoleCodes: [" Platform.Owner "],
      platformPermissionCodes: [" Audit.Read "]);

    Assert.Equal("admin@example.com", user.Email);
    Assert.Equal("Maria Silva", user.DisplayName);
    Assert.True(user.HasMembership(organizationId));
    Assert.Contains(RoleCodes.OrganizationAdmin, user.Memberships[0].RoleCodes);
    Assert.Contains(PermissionCodes.Read(PermissionModules.Properties), user.Memberships[0].PermissionCodes);
    Assert.Contains(RoleCodes.PlatformOwner, user.PlatformRoleCodes);
    Assert.Contains(PermissionCodes.Read(PermissionModules.Audit), user.PlatformPermissionCodes);
  }

  [Fact]
  public void RefreshSessionRecordCapturesPersistenceReadySessionState()
  {
    var createdAt = DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture);
    var expiresAt = createdAt.AddDays(30);
    var revokedAt = createdAt.AddDays(1);

    var session = new RefreshSessionRecord(
      EntityId.New(),
      UserId.New(),
      " token-hash ",
      createdAt,
      expiresAt,
      activeOrganizationId: OrganizationId.New(),
      userAgent: " browser ",
      ipAddress: " 127.0.0.1 ",
      revokedAt: revokedAt,
      revokedReason: " logout ");

    Assert.Equal("token-hash", session.TokenHash);
    Assert.Equal("browser", session.UserAgent);
    Assert.Equal("127.0.0.1", session.IpAddress);
    Assert.Equal("logout", session.RevokedReason);
    Assert.True(session.IsRevoked);
    Assert.False(session.CanRefresh(createdAt.AddMinutes(1)));
  }

  [Fact]
  public void AccessTokenDescriptorRequiresUserAndMembershipCollection()
  {
    var descriptor = new AccessTokenDescriptor(
      UserId.New(),
      [new OrganizationMembership(OrganizationId.New(), permissionCodes: [" residents.write "])],
      email: " user@example.com ");

    Assert.Equal("user@example.com", descriptor.Email);
    Assert.Contains(PermissionCodes.Write(PermissionModules.Residents), descriptor.Memberships[0].PermissionCodes);
  }
}
