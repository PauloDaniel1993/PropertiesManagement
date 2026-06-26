using System.Collections.ObjectModel;
using System.Globalization;

namespace Alsappan.Domain.Common.ValueObjects;

public sealed record LocalizedCatalogLabel
{
  public const string DefaultLocale = "pt-BR";

  public LocalizedCatalogLabel(
    string code,
    IReadOnlyDictionary<string, string> labels,
    string defaultLocale = DefaultLocale)
  {
    Code = NormalizeCode(code);
    DefaultLabelLocale = NormalizeLocale(defaultLocale);
    Labels = NormalizeLabels(labels);

    if (!Labels.ContainsKey(DefaultLabelLocale))
    {
      throw new ArgumentException("Labels must include the default locale.", nameof(labels));
    }
  }

  public string Code { get; }

  public IReadOnlyDictionary<string, string> Labels { get; }

  public string DefaultLabelLocale { get; }

  public string GetLabel(string? locale)
  {
    if (!string.IsNullOrWhiteSpace(locale))
    {
      var normalizedLocale = NormalizeLocale(locale);

      if (Labels.TryGetValue(normalizedLocale, out var localizedLabel))
      {
        return localizedLabel;
      }
    }

    return Labels[DefaultLabelLocale];
  }

  private static string NormalizeCode(string code)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(code);
#pragma warning disable CA1308 // Catalog codes are stored in lowercase for route and API consistency.
    return code.Trim().ToLowerInvariant();
#pragma warning restore CA1308
  }

  private static string NormalizeLocale(string locale)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(locale);
    return CultureInfo.GetCultureInfo(locale.Trim()).Name;
  }

  private static ReadOnlyDictionary<string, string> NormalizeLabels(IReadOnlyDictionary<string, string> labels)
  {
    ArgumentNullException.ThrowIfNull(labels);

    if (labels.Count == 0)
    {
      throw new ArgumentException("At least one localized label is required.", nameof(labels));
    }

    var normalized = new Dictionary<string, string>(StringComparer.Ordinal);

    foreach (var (locale, label) in labels)
    {
      var normalizedLocale = NormalizeLocale(locale);
      ArgumentException.ThrowIfNullOrWhiteSpace(label);
      normalized[normalizedLocale] = label.Trim();
    }

    return new ReadOnlyDictionary<string, string>(normalized);
  }
}
