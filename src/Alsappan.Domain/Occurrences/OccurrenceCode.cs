using System.Globalization;
using System.Text;

namespace Alsappan.Domain.Occurrences;

public static class OccurrenceCode
{
  public static string Required(string? value, int maxLength, string parameterName)
  {
    var normalized = Optional(value, maxLength, parameterName);
    return normalized ?? throw new ArgumentException("Value is required.", parameterName);
  }

  public static string? Optional(string? value, int maxLength, string parameterName)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);

    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    var normalized = value.Trim();
    if (normalized.Length > maxLength)
    {
      throw new ArgumentOutOfRangeException(parameterName, $"Value must be {maxLength} characters or fewer.");
    }

    return normalized;
  }

  public static string NormalizeCode(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
#pragma warning disable CA1308
    return value.Trim().Replace('_', '-').ToLowerInvariant();
#pragma warning restore CA1308
  }

  public static string NormalizeSearchText(params string?[] values)
  {
    ArgumentNullException.ThrowIfNull(values);

    var builder = new StringBuilder();
    foreach (var value in values)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        continue;
      }

      if (builder.Length > 0)
      {
        builder.Append(' ');
      }

      builder.Append(RemoveDiacritics(value.Trim()).ToUpperInvariant());
    }

    return builder.Length <= 4000 ? builder.ToString() : builder.ToString()[..4000];
  }

  private static string RemoveDiacritics(string value)
  {
    var normalized = value.Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(normalized.Length);

    foreach (var character in normalized)
    {
      if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
      {
        builder.Append(character);
      }
    }

    return builder.ToString().Normalize(NormalizationForm.FormC);
  }
}
