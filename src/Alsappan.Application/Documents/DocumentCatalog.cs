using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Documents;

namespace Alsappan.Application.Documents;

public static class DocumentCatalog
{
  public const long MaxFileSizeBytes = 10 * 1024 * 1024;

  private static readonly Dictionary<DocumentCategory, (string Code, string Pt, string En)> Categories = new()
  {
    [DocumentCategory.General] = ("general", "Geral", "General"),
    [DocumentCategory.Property] = ("property", "Imovel", "Property"),
    [DocumentCategory.Contract] = ("contract", "Contrato", "Contract"),
    [DocumentCategory.Resident] = ("resident", "Morador", "Resident"),
    [DocumentCategory.PaymentReceipt] = ("payment-receipt", "Recibo de pagamento", "Payment receipt"),
    [DocumentCategory.UtilityAccount] = ("utility-account", "Conta de consumo", "Utility account"),
    [DocumentCategory.Pet] = ("pet", "Pet", "Pet"),
    [DocumentCategory.Vehicle] = ("vehicle", "Veiculo", "Vehicle"),
    [DocumentCategory.Occurrence] = ("occurrence", "Ocorrencia", "Occurrence"),
    [DocumentCategory.Inspection] = ("inspection", "Vistoria", "Inspection"),
    [DocumentCategory.Other] = ("other", "Outro", "Other")
  };

  private static readonly Dictionary<DocumentStatus, (string Code, string Pt, string En, string Tone)> Statuses = new()
  {
    [DocumentStatus.Active] = ("active", "Ativo", "Active", StatusLabelTones.Success),
    [DocumentStatus.Archived] = ("archived", "Arquivado", "Archived", "archived")
  };

  private static readonly HashSet<string> SupportedEntityTypes = new(StringComparer.Ordinal)
  {
    "property",
    "contract",
    "resident",
    "payment",
    "utility-account",
    "pet",
    "vehicle",
    "occurrence",
    "inspection"
  };

  private static readonly Dictionary<string, string[]> AllowedContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
  {
    [".pdf"] = ["application/pdf"],
    [".png"] = ["image/png"],
    [".jpg"] = ["image/jpeg"],
    [".jpeg"] = ["image/jpeg"],
    [".webp"] = ["image/webp"],
    [".txt"] = ["text/plain"],
    [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
    [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
  };

  public static IReadOnlyList<SelectOptionDto> GetCategoryOptions(string? locale = null) =>
    Categories
      .OrderBy(category => category.Value.Pt, StringComparer.OrdinalIgnoreCase)
      .Select(category => new SelectOptionDto(category.Value.Code, IsPortuguese(locale) ? category.Value.Pt : category.Value.En))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetStatusOptions(string? locale = null) =>
    Statuses
      .Select(status => new StatusLabelDto(
        status.Value.Code,
        IsPortuguese(locale) ? status.Value.Pt : status.Value.En,
        status.Value.Tone))
      .ToArray();

  public static IReadOnlyList<SelectOptionDto> GetAllowedFileTypeOptions(string? locale = null)
  {
    var portuguese = IsPortuguese(locale);
    return AllowedContentTypesByExtension.Keys
      .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
      .Select(extension => new SelectOptionDto(extension, portuguese ? $"Arquivo {extension}" : $"{extension} file"))
      .ToArray();
  }

  public static bool TryParseCategory(string? value, out DocumentCategory category)
  {
    var code = string.IsNullOrWhiteSpace(value)
      ? string.Empty
      : DocumentCode.NormalizeCode(value);
    foreach (var (candidate, metadata) in Categories)
    {
      if (string.Equals(metadata.Code, code, StringComparison.Ordinal))
      {
        category = candidate;
        return true;
      }
    }

    category = DocumentCategory.General;
    return false;
  }

  public static bool TryParseStatus(string? value, out DocumentStatus status)
  {
    var code = string.IsNullOrWhiteSpace(value)
      ? string.Empty
      : DocumentCode.NormalizeCode(value);
    foreach (var (candidate, metadata) in Statuses)
    {
      if (string.Equals(metadata.Code, code, StringComparison.Ordinal))
      {
        status = candidate;
        return true;
      }
    }

    status = DocumentStatus.Active;
    return false;
  }

  public static string ToCategoryCode(DocumentCategory category) => Categories[category].Code;

  public static string ToStatusCode(DocumentStatus status) => Statuses[status].Code;

  public static string GetCategoryLabel(DocumentCategory category, string? locale = null) =>
    IsPortuguese(locale) ? Categories[category].Pt : Categories[category].En;

  public static StatusLabelDto GetStatusLabel(DocumentStatus status, string? locale = null)
  {
    var metadata = Statuses[status];
    return new StatusLabelDto(metadata.Code, IsPortuguese(locale) ? metadata.Pt : metadata.En, metadata.Tone);
  }

  public static bool IsSupportedEntityType(string entityType) =>
    SupportedEntityTypes.Contains(DocumentCode.NormalizeCode(entityType));

  public static bool IsAllowedFile(string fileName, string contentType)
  {
    if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
    {
      return false;
    }

    var extension = Path.GetExtension(fileName);
    return !string.IsNullOrWhiteSpace(extension) &&
      AllowedContentTypesByExtension.TryGetValue(extension, out var contentTypes) &&
      contentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
  }

  public static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) || locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
}
