using System.Security.Cryptography;
using Alsappan.Application.Identity.Security;
using Alsappan.Domain.Identity;

namespace Alsappan.Infrastructure.Identity;

public sealed class Pbkdf2PasswordHashService : IPasswordHashService
{
  private const string Marker = "ALSPBKDF2";
  private const int Version = 1;
  private const int SaltByteLength = 16;
  private const int SubkeyByteLength = 32;
  private const int IterationCount = 100_000;

  public string HashPassword(IdentityUser user, string password)
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentException.ThrowIfNullOrWhiteSpace(password);

    var salt = RandomNumberGenerator.GetBytes(SaltByteLength);
    var subkey = Rfc2898DeriveBytes.Pbkdf2(
      password,
      salt,
      IterationCount,
      HashAlgorithmName.SHA256,
      SubkeyByteLength);

    return string.Join(
      '$',
      Marker,
      Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
      IterationCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
      Base64UrlEncode(salt),
      Base64UrlEncode(subkey));
  }

  public PasswordVerificationResult VerifyPassword(
    IdentityUser user,
    string password,
    string passwordHash)
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentException.ThrowIfNullOrWhiteSpace(password);
    ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

    var parts = passwordHash.Split('$');
    if (parts.Length != 5 ||
      !string.Equals(parts[0], Marker, StringComparison.Ordinal) ||
      !int.TryParse(parts[1], out var version) ||
      version != Version ||
      !int.TryParse(parts[2], out var iterations) ||
      iterations < 1)
    {
      return PasswordVerificationResult.Failed;
    }

    byte[] salt;
    byte[] expectedSubkey;
    try
    {
      salt = Base64UrlDecode(parts[3]);
      expectedSubkey = Base64UrlDecode(parts[4]);
    }
    catch (FormatException)
    {
      return PasswordVerificationResult.Failed;
    }

    var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
      password,
      salt,
      iterations,
      HashAlgorithmName.SHA256,
      expectedSubkey.Length);

    if (!CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey))
    {
      return PasswordVerificationResult.Failed;
    }

    return iterations < IterationCount
      ? PasswordVerificationResult.SuccessRehashNeeded
      : PasswordVerificationResult.Success;
  }

  private static string Base64UrlEncode(byte[] bytes) =>
    Convert.ToBase64String(bytes)
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '_');

  private static byte[] Base64UrlDecode(string value)
  {
    var padded = value.Replace('-', '+').Replace('_', '/');
    padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
    return Convert.FromBase64String(padded);
  }
}
