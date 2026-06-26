using System.Security.Cryptography;
using System.Text;
using Alsappan.Application.Common.Auth;

namespace Alsappan.Infrastructure.Auth;

public sealed class Sha256RefreshTokenProtector : IRefreshTokenProtector
{
  private const int TokenByteLength = 64;

  public RefreshTokenSecret CreateToken()
  {
    var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenByteLength));
    return new RefreshTokenSecret(token, HashToken(token));
  }

  public string HashToken(string refreshToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
    return Base64UrlEncode(hash);
  }

  public bool TokenMatchesHash(string refreshToken, string tokenHash)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
    ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

    var computedHash = Encoding.UTF8.GetBytes(HashToken(refreshToken));
    var persistedHash = Encoding.UTF8.GetBytes(tokenHash.Trim());

    return computedHash.Length == persistedHash.Length
      && CryptographicOperations.FixedTimeEquals(computedHash, persistedHash);
  }

  private static string Base64UrlEncode(byte[] bytes) =>
    Convert.ToBase64String(bytes)
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '_');
}
