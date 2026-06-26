namespace Alsappan.Application.Common.Auth;

public static class AuthTransportConstants
{
  public const string BearerScheme = "Bearer";
  public const string AccessTokenHeaderValuePrefix = "Bearer ";
  public const string DefaultAccessTokenHeaderName = "Authorization";
  public const string DefaultRefreshCookieName = "__Host-alsappan-refresh";
  public const bool RefreshCookieHttpOnly = true;
  public const bool RefreshCookieSecure = true;
  public const string RefreshCookieSameSite = "Strict";
  public const string RefreshCookiePath = "/";
}
