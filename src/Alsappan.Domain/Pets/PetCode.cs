using System.Globalization;

namespace Alsappan.Domain.Pets;

public static class PetCode
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

  public static string NormalizeSearchText(params string?[] values)
  {
    var searchText = string.Join(
      ' ',
      values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!.Trim()));

    return searchText.Length <= 4000
      ? searchText.ToUpper(CultureInfo.InvariantCulture)
      : searchText[..4000].ToUpper(CultureInfo.InvariantCulture);
  }
}
