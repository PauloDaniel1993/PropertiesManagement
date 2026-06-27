using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;

namespace Alsappan.Application.Contracts;

public static class ContractCatalog
{
  private static readonly LocalizedCatalogLabel[] StatusLabels =
  [
    new(
      ToStatusCode(ContractStatus.Draft),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Rascunho",
        ["en-US"] = "Draft"
      }),
    new(
      ToStatusCode(ContractStatus.Active),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Ativo",
        ["en-US"] = "Active"
      }),
    new(
      ToStatusCode(ContractStatus.EndingSoon),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "A vencer",
        ["en-US"] = "Ending soon"
      }),
    new(
      ToStatusCode(ContractStatus.Ended),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Encerrado",
        ["en-US"] = "Ended"
      }),
    new(
      ToStatusCode(ContractStatus.Terminated),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Rescindido",
        ["en-US"] = "Terminated"
      }),
    new(
      ToStatusCode(ContractStatus.Cancelled),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Cancelado",
        ["en-US"] = "Cancelled"
      }),
    new(
      ToStatusCode(ContractStatus.Archived),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Arquivado",
        ["en-US"] = "Archived"
      })
  ];

  private static readonly LocalizedCatalogLabel[] AdjustmentLabels =
  [
    new(
      ToAdjustmentIndexCode(ContractAdjustmentIndex.Igpm),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "IGP-M",
        ["en-US"] = "IGP-M"
      }),
    new(
      ToAdjustmentIndexCode(ContractAdjustmentIndex.Ipca),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "IPCA",
        ["en-US"] = "IPCA"
      }),
    new(
      ToAdjustmentIndexCode(ContractAdjustmentIndex.Fixed),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Percentual fixo",
        ["en-US"] = "Fixed percentage"
      }),
    new(
      ToAdjustmentIndexCode(ContractAdjustmentIndex.Other),
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Outro",
        ["en-US"] = "Other"
      })
  ];

  public static IReadOnlyList<StatusLabelDto> GetStatusOptions(string? locale = null) =>
    StatusLabels.Select(label => new StatusLabelDto(label.Code, label.GetLabel(locale), ToStatusTone(label.Code)))
      .ToArray();

  public static IReadOnlyList<SelectOptionDto> GetAdjustmentIndexOptions(string? locale = null) =>
    AdjustmentLabels.Select(label => new SelectOptionDto(label.Code, label.GetLabel(locale))).ToArray();

  public static StatusLabelDto GetStatusLabel(ContractStatus status, string? locale = null)
  {
    var code = ToStatusCode(status);
    var label = StatusLabels.FirstOrDefault(candidate => candidate.Code == code);
    return new StatusLabelDto(code, label?.GetLabel(locale) ?? code, ToStatusTone(code));
  }

  public static string GetAdjustmentIndexLabel(ContractAdjustmentIndex index, string? locale = null)
  {
    var code = ToAdjustmentIndexCode(index);
    return AdjustmentLabels.FirstOrDefault(candidate => candidate.Code == code)?.GetLabel(locale) ?? code;
  }

  public static string ToStatusCode(ContractStatus status) =>
    status switch
    {
      ContractStatus.Draft => "draft",
      ContractStatus.Active => "active",
      ContractStatus.EndingSoon => "ending-soon",
      ContractStatus.Ended => "ended",
      ContractStatus.Terminated => "terminated",
      ContractStatus.Cancelled => "cancelled",
      ContractStatus.Archived => "archived",
      _ => string.Empty
    };

  public static string ToAdjustmentIndexCode(ContractAdjustmentIndex index) =>
    index switch
    {
      ContractAdjustmentIndex.Igpm => "igpm",
      ContractAdjustmentIndex.Ipca => "ipca",
      ContractAdjustmentIndex.Fixed => "fixed",
      ContractAdjustmentIndex.Other => "other",
      _ => string.Empty
    };

  public static bool TryParseStatus(string? value, out ContractStatus status)
  {
    status = ContractStatus.None;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    status = ContractCode.NormalizeCode(value) switch
    {
      "draft" => ContractStatus.Draft,
      "active" => ContractStatus.Active,
      "ending-soon" => ContractStatus.EndingSoon,
      "ended" => ContractStatus.Ended,
      "terminated" => ContractStatus.Terminated,
      "cancelled" => ContractStatus.Cancelled,
      "archived" => ContractStatus.Archived,
      _ => ContractStatus.None
    };

    return status != ContractStatus.None;
  }

  public static bool TryParseStoredStatus(string? value, out ContractStatus status) =>
    TryParseStatus(value, out status) && status is not ContractStatus.EndingSoon and not ContractStatus.Ended;

  public static bool TryParseAdjustmentIndex(string? value, out ContractAdjustmentIndex index)
  {
    index = ContractAdjustmentIndex.None;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    index = ContractCode.NormalizeCode(value) switch
    {
      "igpm" or "igp-m" => ContractAdjustmentIndex.Igpm,
      "ipca" => ContractAdjustmentIndex.Ipca,
      "fixed" or "fixed-percentage" => ContractAdjustmentIndex.Fixed,
      "other" => ContractAdjustmentIndex.Other,
      _ => ContractAdjustmentIndex.None
    };

    return index != ContractAdjustmentIndex.None;
  }

  public static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) || locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);

  private static string ToStatusTone(string code) =>
    code switch
    {
      "active" => StatusLabelTones.Success,
      "ending-soon" => StatusLabelTones.Warning,
      "ended" or "terminated" or "cancelled" => StatusLabelTones.Danger,
      "archived" => StatusLabelTones.Danger,
      _ => StatusLabelTones.Neutral
    };
}
