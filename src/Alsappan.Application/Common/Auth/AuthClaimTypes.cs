namespace Alsappan.Application.Common.Auth;

public static class AuthClaimTypes
{
  public const string UserId = "sub";
  public const string Email = "email";
  public const string DisplayName = "name";
  public const string SessionId = "sid";
  public const string ActiveOrganizationId = "alsappan:active_organization_id";
  public const string Membership = "alsappan:membership";
  public const string OrganizationRole = "alsappan:role";
  public const string OrganizationPermission = "alsappan:permission";
  public const string PlatformRole = "alsappan:platform_role";
  public const string PlatformPermission = "alsappan:platform_permission";
}
