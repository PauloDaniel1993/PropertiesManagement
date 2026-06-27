namespace Alsappan.Domain.Identity;

public static class IdentityDefaults
{
  public const string DefaultLocale = "pt-BR";
  public const string DefaultCurrency = "BRL";
  public const int DefaultMaxFailedAccessAttempts = 5;
  public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);
}
