namespace Alsappan.Domain.Identity;

public static class IdentityCode
{
  public static string NormalizeCode(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);

#pragma warning disable CA1308
    return value.Trim().ToLowerInvariant();
#pragma warning restore CA1308
  }

  public static string NormalizeEmail(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);

#pragma warning disable CA1308
    return value.Trim().ToLowerInvariant();
#pragma warning restore CA1308
  }

  public static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    return value.Trim();
  }

  public static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  public static string[] NormalizeCodes(IEnumerable<string>? values)
  {
    var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var value in values ?? [])
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
        codes.Add(NormalizeCode(value));
      }
    }

    return codes
      .Order(StringComparer.OrdinalIgnoreCase)
      .ToArray();
  }
}
