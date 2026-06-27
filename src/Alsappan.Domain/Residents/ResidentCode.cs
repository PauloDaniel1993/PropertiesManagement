using System.Globalization;
using System.Text;

namespace Alsappan.Domain.Residents;

public static class ResidentCode
{
  public static string Required(string value, string parameterName, int maxLength)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

    var trimmed = value.Trim();
    if (trimmed.Length > maxLength)
    {
      throw new ArgumentException($"Value must be {maxLength} characters or fewer.", parameterName);
    }

    return trimmed;
  }

  public static string? Optional(string? value, int maxLength, string parameterName)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    var trimmed = value.Trim();
    if (trimmed.Length > maxLength)
    {
      throw new ArgumentException($"Value must be {maxLength} characters or fewer.", parameterName);
    }

    return trimmed;
  }

  public static string? NormalizeEmail(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

#pragma warning disable CA1308
    return value.Trim().ToLowerInvariant();
#pragma warning restore CA1308
  }

  public static string? NormalizePhone(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    var builder = new StringBuilder(value.Length);
    foreach (var character in value)
    {
      if (char.IsLetterOrDigit(character))
      {
        builder.Append(char.ToUpperInvariant(character));
      }
    }

    return builder.Length == 0 ? null : builder.ToString();
  }

  public static string? NormalizeIdentifier(string? value) => NormalizePhone(value);

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

    return builder.ToString();
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
