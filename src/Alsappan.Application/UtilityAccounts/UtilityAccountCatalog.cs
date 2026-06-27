using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.UtilityAccounts;

namespace Alsappan.Application.UtilityAccounts;

public static class UtilityAccountCatalog
{
  private static readonly Dictionary<UtilityAccountStatus, (string Code, string Pt, string En, string Tone)> Statuses =
    new()
    {
      [UtilityAccountStatus.Open] = ("open", "Em aberto", "Open", "warning"),
      [UtilityAccountStatus.Overdue] = ("overdue", "Vencida", "Overdue", "danger"),
      [UtilityAccountStatus.Paid] = ("paid", "Paga", "Paid", "success"),
      [UtilityAccountStatus.Cancelled] = ("cancelled", "Cancelada", "Cancelled", "archived"),
      [UtilityAccountStatus.Disputed] = ("disputed", "Em disputa", "Disputed", "danger"),
      [UtilityAccountStatus.Archived] = ("archived", "Arquivada", "Archived", "archived")
    };

  private static readonly Dictionary<UtilityAccountType, (string Code, string Pt, string En, string Tone)> Types =
    new()
    {
      [UtilityAccountType.Electricity] = ("electricity", "Energia", "Electricity", "warning"),
      [UtilityAccountType.Water] = ("water", "Agua", "Water", "info"),
      [UtilityAccountType.Gas] = ("gas", "Gas", "Gas", "warning"),
      [UtilityAccountType.Internet] = ("internet", "Internet", "Internet", "info"),
      [UtilityAccountType.CondominiumFee] = ("condominium-fee", "Condominio", "Condominium fee", "neutral"),
      [UtilityAccountType.Iptu] = ("iptu", "IPTU", "IPTU", "neutral"),
      [UtilityAccountType.Insurance] = ("insurance", "Seguro", "Insurance", "neutral"),
      [UtilityAccountType.Other] = ("other", "Outra", "Other", "neutral")
    };

  private static readonly Dictionary<UtilityResponsibility, (string Code, string Pt, string En, string Tone)> Responsibilities =
    new()
    {
      [UtilityResponsibility.Organization] = ("organization", "Organizacao", "Organization", "neutral"),
      [UtilityResponsibility.Property] = ("property", "Imovel", "Property", "info"),
      [UtilityResponsibility.Contract] = ("contract", "Contrato", "Contract", "success"),
      [UtilityResponsibility.Resident] = ("resident", "Morador", "Resident", "warning"),
      [UtilityResponsibility.Owner] = ("owner", "Proprietario", "Owner", "neutral"),
      [UtilityResponsibility.Other] = ("other", "Outro", "Other", "neutral")
    };

  public static IReadOnlyList<StatusLabelDto> GetStatusOptions(string? locale = null) =>
    Statuses
      .OrderBy(status => status.Value.Code, StringComparer.Ordinal)
      .Select(status => ToStatusLabel(status.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetTypeOptions(string? locale = null) =>
    Types
      .OrderBy(type => type.Value.Code, StringComparer.Ordinal)
      .Select(type => ToTypeLabel(type.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetResponsibilityOptions(string? locale = null) =>
    Responsibilities
      .OrderBy(responsibility => responsibility.Value.Code, StringComparer.Ordinal)
      .Select(responsibility => ToResponsibilityLabel(responsibility.Key, locale))
      .ToArray();

  public static StatusLabelDto ToStatusLabel(UtilityAccountStatus status, string? locale = null)
  {
    var labels = Statuses[status];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToTypeLabel(UtilityAccountType type, string? locale = null)
  {
    var labels = Types[type];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToResponsibilityLabel(UtilityResponsibility responsibility, string? locale = null)
  {
    var labels = Responsibilities[responsibility];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static bool TryParseStatus(string? code, out UtilityAccountStatus status) =>
    TryParse(code, Statuses, out status);

  public static bool TryParseType(string? code, out UtilityAccountType type) =>
    TryParse(code, Types, out type);

  public static bool TryParseResponsibility(string? code, out UtilityResponsibility responsibility) =>
    TryParse(code, Responsibilities, out responsibility);

  public static string GetDocumentKindLabel(UtilityDocumentKind kind, string? locale = null)
  {
    var english = IsEnglish(locale);
    return kind == UtilityDocumentKind.Receipt
      ? english ? "Receipt" : "Recibo"
      : english ? "Bill" : "Conta";
  }

  private static bool TryParse<TEnum>(
    string? code,
    IReadOnlyDictionary<TEnum, (string Code, string Pt, string En, string Tone)> values,
    out TEnum value)
    where TEnum : struct, Enum
  {
    foreach (var candidate in values)
    {
      if (string.Equals(candidate.Value.Code, code, StringComparison.OrdinalIgnoreCase))
      {
        value = candidate.Key;
        return true;
      }
    }

    value = default;
    return false;
  }

  private static bool IsEnglish(string? locale) =>
    locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
}
