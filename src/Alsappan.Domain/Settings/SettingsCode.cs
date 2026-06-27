using System.Globalization;

namespace Alsappan.Domain.Settings;

public static class SettingsCode
{
  public static string NormalizeCode(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);

#pragma warning disable CA1308
    return value.Trim().ToLowerInvariant();
#pragma warning restore CA1308
  }

  public static string NormalizeLocale(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    return CultureInfo.GetCultureInfo(value.Trim()).Name;
  }

  public static string[] NormalizeLocales(IEnumerable<string>? values)
  {
    var locales = new HashSet<string>(StringComparer.Ordinal);
    foreach (var value in values ?? [])
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
        locales.Add(NormalizeLocale(value));
      }
    }

    return locales.Order(StringComparer.Ordinal).ToArray();
  }

  public static string Required(string value, string parameterName, int maxLength = 180)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    var normalized = value.Trim();
    return normalized.Length <= maxLength
      ? normalized
      : throw new ArgumentOutOfRangeException(parameterName, $"Value must be {maxLength} characters or fewer.");
  }

  public static string? Optional(string? value, int maxLength = 180, string? parameterName = null)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    var normalized = value.Trim();
    return normalized.Length <= maxLength
      ? normalized
      : throw new ArgumentOutOfRangeException(parameterName ?? nameof(value), $"Value must be {maxLength} characters or fewer.");
  }
}
