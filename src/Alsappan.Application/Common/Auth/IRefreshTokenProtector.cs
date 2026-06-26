namespace Alsappan.Application.Common.Auth;

public interface IRefreshTokenProtector
{
  RefreshTokenSecret CreateToken();

  string HashToken(string refreshToken);

  bool TokenMatchesHash(string refreshToken, string tokenHash);
}
