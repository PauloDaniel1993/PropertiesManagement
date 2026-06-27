using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Notifications;
using Alsappan.Domain.Settings;

namespace Alsappan.Application.Settings;

public static class SettingsCatalog
{
  public const string PropertyTypes = "property-types";
  public const string OccurrenceTypes = "occurrence-types";
  public const string InspectionTypes = "inspection-types";
  public const string DocumentCategories = "document-categories";
  public const string UtilityTypes = "utility-types";
  public const string ConfigurableLabels = "configurable-labels";

  public const int MaxLogoSizeBytes = 2 * 1024 * 1024;
  public const int MaxLogoDimensionPixels = 4096;

  public static readonly string[] SupportedLocales = ["pt-BR", "en-US"];

  public static readonly string[] SupportedMfaPolicies = ["disabled", "optional", "required"];

  public static readonly string[] SupportedLogoContentTypes =
  [
    "image/jpeg",
    "image/png",
    "image/webp"
  ];

  public static IReadOnlyList<DefaultCatalogItem> DefaultCatalogItems { get; } =
  [
    new(PropertyTypes, "apartment", "Apartamento", "Apartment", 10),
    new(PropertyTypes, "house", "Casa", "House", 20),
    new(PropertyTypes, "commercial-room", "Sala comercial", "Commercial room", 30),
    new(PropertyTypes, "land", "Terreno", "Land", 40),
    new(PropertyTypes, "other", "Outro", "Other", 50),
    new(OccurrenceTypes, "maintenance", "Manutencao", "Maintenance", 10),
    new(OccurrenceTypes, "complaint", "Reclamacao", "Complaint", 20),
    new(OccurrenceTypes, "incident", "Incidente", "Incident", 30),
    new(OccurrenceTypes, "request", "Solicitacao", "Request", 40),
    new(OccurrenceTypes, "other", "Outro", "Other", 50),
    new(InspectionTypes, "move-in", "Entrada", "Move-in", 10),
    new(InspectionTypes, "move-out", "Saida", "Move-out", 20),
    new(InspectionTypes, "periodic", "Periodica", "Periodic", 30),
    new(InspectionTypes, "maintenance", "Manutencao", "Maintenance", 40),
    new(InspectionTypes, "other", "Outra", "Other", 50),
    new(DocumentCategories, "general", "Geral", "General", 10),
    new(DocumentCategories, "property", "Imovel", "Property", 20),
    new(DocumentCategories, "contract", "Contrato", "Contract", 30),
    new(DocumentCategories, "resident", "Morador", "Resident", 40),
    new(DocumentCategories, "payment-receipt", "Recibo de pagamento", "Payment receipt", 50),
    new(DocumentCategories, "utility-account", "Conta de consumo", "Utility account", 60),
    new(DocumentCategories, "pet", "Pet", "Pet", 70),
    new(DocumentCategories, "vehicle", "Veiculo", "Vehicle", 80),
    new(DocumentCategories, "occurrence", "Ocorrencia", "Occurrence", 90),
    new(DocumentCategories, "inspection", "Vistoria", "Inspection", 100),
    new(DocumentCategories, "other", "Outro", "Other", 110),
    new(UtilityTypes, "electricity", "Energia", "Electricity", 10),
    new(UtilityTypes, "water", "Agua", "Water", 20),
    new(UtilityTypes, "gas", "Gas", "Gas", 30),
    new(UtilityTypes, "internet", "Internet", "Internet", 40),
    new(UtilityTypes, "condominium-fee", "Condominio", "Condominium fee", 50),
    new(UtilityTypes, "iptu", "IPTU", "IPTU", 60),
    new(UtilityTypes, "insurance", "Seguro", "Insurance", 70),
    new(UtilityTypes, "other", "Outra", "Other", 80),
    new(ConfigurableLabels, "owner", "Proprietario", "Owner", 10),
    new(ConfigurableLabels, "management-company", "Administradora", "Management company", 20),
    new(ConfigurableLabels, "resident", "Morador", "Resident", 30)
  ];

  public static IReadOnlyList<string> CatalogTypes { get; } =
    DefaultCatalogItems
      .Select(item => item.CatalogType)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal)
      .ToArray();

  public static IReadOnlyList<string> NotificationCategories => NotificationCatalog.CategoryCodes;

  public static IReadOnlyList<string> NotificationChannels => ["in-app", "email", "whatsapp"];

  public static string NormalizeCatalogType(string catalogType) => SettingsCode.NormalizeCode(catalogType);

  public static bool IsKnownCatalogType(string? catalogType) =>
    !string.IsNullOrWhiteSpace(catalogType) &&
      CatalogTypes.Contains(NormalizeCatalogType(catalogType), StringComparer.Ordinal);

  public static string CatalogTypeLabel(string catalogType, string? locale = null)
  {
    var portuguese = IsPortuguese(locale);
    return NormalizeCatalogType(catalogType) switch
    {
      PropertyTypes => portuguese ? "Tipos de imovel" : "Property types",
      OccurrenceTypes => portuguese ? "Tipos de ocorrencia" : "Occurrence types",
      InspectionTypes => portuguese ? "Tipos de vistoria" : "Inspection types",
      DocumentCategories => portuguese ? "Categorias de documento" : "Document categories",
      UtilityTypes => portuguese ? "Tipos de conta" : "Utility types",
      ConfigurableLabels => portuguese ? "Rotulos configuraveis" : "Configurable labels",
      _ => catalogType
    };
  }

  public static IReadOnlyList<LocaleOptionDto> BuildLocaleOptions(
    IEnumerable<string> enabledLocales,
    string defaultLocale,
    string fallbackLocale,
    string? locale = null)
  {
    var enabled = new HashSet<string>(
      enabledLocales.Select(SettingsCode.NormalizeLocale),
      StringComparer.Ordinal);
    var normalizedDefault = SettingsCode.NormalizeLocale(defaultLocale);
    var normalizedFallback = SettingsCode.NormalizeLocale(fallbackLocale);

    return SupportedLocales
      .Select(code => new LocaleOptionDto(
        code,
        LocaleLabel(code, locale),
        enabled.Contains(code),
        string.Equals(code, normalizedDefault, StringComparison.Ordinal),
        string.Equals(code, normalizedFallback, StringComparison.Ordinal)))
      .ToArray();
  }

  public static IReadOnlyList<NotificationSettingOptionDto> BuildNotificationOptions(
    IEnumerable<string> allCodes,
    IEnumerable<string> enabledCodes,
    Func<string, string?> getLabel)
  {
    var enabled = new HashSet<string>(
      enabledCodes.Select(SettingsCode.NormalizeCode),
      StringComparer.OrdinalIgnoreCase);

    return allCodes
      .Select(SettingsCode.NormalizeCode)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .Order(StringComparer.OrdinalIgnoreCase)
      .Select(code => new NotificationSettingOptionDto(code, getLabel(code) ?? code, enabled.Contains(code)))
      .ToArray();
  }

  public static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) ||
      locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);

  public static string LocaleLabel(string localeCode, string? displayLocale = null)
  {
    var english = !IsPortuguese(displayLocale);
    return SettingsCode.NormalizeLocale(localeCode) switch
    {
      "en-US" => english ? "English (United States)" : "Ingles (Estados Unidos)",
      _ => english ? "Portuguese (Brazil)" : "Portugues (Brasil)"
    };
  }
}

public sealed record DefaultCatalogItem(
  string CatalogType,
  string Code,
  string LabelPtBr,
  string LabelEnUs,
  int SortOrder);
