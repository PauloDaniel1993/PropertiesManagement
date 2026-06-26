namespace Alsappan.Application.Common.Auth;

public interface IAccessTokenService
{
  IssuedAccessToken IssueToken(AccessTokenDescriptor descriptor);
}
