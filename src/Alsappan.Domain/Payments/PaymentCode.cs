using System.Globalization;
using System.Text;

namespace Alsappan.Domain.Payments;

public static class PaymentCode
{
  public const int MaxSearchTextLength = 4000;

  public static string NormalizeCode(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);

#pragma warning disable CA1308
    var normalized = value.Trim().Replace('_', '-').Replace(' ', '-').ToLowerInvariant();
#pragma warning restore CA1308
    while (normalized.Contains("--", StringComparison.Ordinal))
    {
      normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
    }

    return normalized;
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
      throw new ArgumentOutOfRangeException(parameterName, $"Value cannot exceed {maxLength} characters.");
    }

    return trimmed;
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

      builder.Append(RemoveDiacritics(value).Trim().ToUpperInvariant());
    }

    var normalizedSearchText = builder.ToString();
    if (normalizedSearchText.Length <= MaxSearchTextLength)
    {
      return normalizedSearchText;
    }

    var truncatedSearchText = normalizedSearchText[..MaxSearchTextLength];
    return char.IsHighSurrogate(truncatedSearchText[^1])
      ? truncatedSearchText[..^1]
      : truncatedSearchText;
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
