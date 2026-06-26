using System.Collections.ObjectModel;

namespace Alsappan.Application.Common.Configuration;

public sealed class AlsappanOptions
{
  public const string SectionName = "Alsappan";

  public DatabaseOptions Database { get; init; } = new();

  public AuthOptions Auth { get; init; } = new();

  public StorageOptions Storage { get; init; } = new();

  public LocalizationOptions Localization { get; init; } = new();

  public CorsOptions Cors { get; init; } = new();
}

public sealed class DatabaseOptions
{
  public string ConnectionString { get; init; } = string.Empty;

  public string Schema { get; init; } = "app";
}

public sealed class AuthOptions
{
  public string Issuer { get; init; } = "Alsappan";

  public string Audience { get; init; } = "Alsappan.Web";

  public string SigningKey { get; init; } = string.Empty;

  public int AccessTokenMinutes { get; init; } = 15;

  public int RefreshTokenDays { get; init; } = 30;

  public string RefreshCookieName { get; init; } = "__Host-alsappan-refresh";

  public string AccessTokenHeaderName { get; init; } = "Authorization";
}

public sealed class StorageOptions
{
  public string LocalPath { get; init; } = "./storage";
}

public sealed class LocalizationOptions
{
  public string DefaultCulture { get; init; } = "pt-BR";

  public Collection<string> SupportedCultures { get; init; } = ["pt-BR", "en-US"];
}

public sealed class CorsOptions
{
  public Collection<string> AllowedOrigins { get; init; } = ["http://localhost:5173", "https://localhost:5173"];
}
