using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Inspections;

namespace Alsappan.Application.Inspections;

public static class InspectionCatalog
{
  private static readonly Dictionary<InspectionType, (string Code, string Pt, string En, string Tone)> Types =
    new()
    {
      [InspectionType.MoveIn] = ("move-in", "Entrada", "Move-in", StatusLabelTones.Success),
      [InspectionType.MoveOut] = ("move-out", "Saida", "Move-out", StatusLabelTones.Warning),
      [InspectionType.Periodic] = ("periodic", "Periodica", "Periodic", StatusLabelTones.Info),
      [InspectionType.Maintenance] = ("maintenance", "Manutencao", "Maintenance", StatusLabelTones.Warning),
      [InspectionType.Inventory] = ("inventory", "Inventario", "Inventory", StatusLabelTones.Neutral),
      [InspectionType.Other] = ("other", "Outra", "Other", StatusLabelTones.Neutral)
    };

  private static readonly Dictionary<InspectionStatus, (string Code, string Pt, string En, string Tone)> Statuses =
    new()
    {
      [InspectionStatus.Scheduled] = ("scheduled", "Agendada", "Scheduled", StatusLabelTones.Info),
      [InspectionStatus.InProgress] = ("in-progress", "Em andamento", "In progress", StatusLabelTones.Warning),
      [InspectionStatus.Completed] = ("completed", "Concluida", "Completed", StatusLabelTones.Success),
      [InspectionStatus.Cancelled] = ("cancelled", "Cancelada", "Cancelled", StatusLabelTones.Danger),
      [InspectionStatus.Archived] = ("archived", "Arquivada", "Archived", "archived")
    };

  private static readonly Dictionary<InspectionConditionRating, (string Code, string Pt, string En, string Tone)> Ratings =
    new()
    {
      [InspectionConditionRating.Pending] = ("pending", "Pendente", "Pending", StatusLabelTones.Warning),
      [InspectionConditionRating.Good] = ("good", "Bom", "Good", StatusLabelTones.Success),
      [InspectionConditionRating.Attention] = ("attention", "Atencao", "Attention", StatusLabelTones.Warning),
      [InspectionConditionRating.Damaged] = ("damaged", "Danificado", "Damaged", StatusLabelTones.Danger),
      [InspectionConditionRating.Critical] = ("critical", "Critico", "Critical", StatusLabelTones.Danger),
      [InspectionConditionRating.NotApplicable] = ("not-applicable", "Nao se aplica", "Not applicable", StatusLabelTones.Neutral)
    };

  private static readonly Dictionary<InspectionDocumentKind, (string Code, string Pt, string En, string Tone)> DocumentKinds =
    new()
    {
      [InspectionDocumentKind.Photo] = ("photo", "Foto", "Photo", StatusLabelTones.Info),
      [InspectionDocumentKind.Attachment] = ("attachment", "Anexo", "Attachment", StatusLabelTones.Neutral),
      [InspectionDocumentKind.Report] = ("report", "Relatorio", "Report", StatusLabelTones.Success),
      [InspectionDocumentKind.Signature] = ("signature", "Assinatura", "Signature", StatusLabelTones.Warning)
    };

  public static IReadOnlyList<StatusLabelDto> GetTypeOptions(string? locale = null) =>
    Types
      .OrderBy(type => type.Value.Code, StringComparer.Ordinal)
      .Select(type => ToTypeLabel(type.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetStatusOptions(string? locale = null) =>
    Statuses
      .OrderBy(status => status.Value.Code, StringComparer.Ordinal)
      .Select(status => ToStatusLabel(status.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetConditionRatingOptions(string? locale = null) =>
    Ratings
      .OrderBy(rating => rating.Value.Code, StringComparer.Ordinal)
      .Select(rating => ToConditionRatingLabel(rating.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetDocumentKindOptions(string? locale = null) =>
    DocumentKinds
      .OrderBy(kind => kind.Value.Code, StringComparer.Ordinal)
      .Select(kind => ToDocumentKindLabel(kind.Key, locale))
      .ToArray();

  public static StatusLabelDto ToTypeLabel(InspectionType type, string? locale = null)
  {
    var labels = Types[type];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToStatusLabel(InspectionStatus status, string? locale = null)
  {
    var labels = Statuses[status];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToConditionRatingLabel(
    InspectionConditionRating rating,
    string? locale = null)
  {
    var labels = Ratings[rating];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToDocumentKindLabel(InspectionDocumentKind kind, string? locale = null)
  {
    var labels = DocumentKinds[kind];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static bool TryParseType(string? code, out InspectionType type) =>
    TryParse(code, Types, out type);

  public static bool TryParseStatus(string? code, out InspectionStatus status) =>
    TryParse(code, Statuses, out status);

  public static bool TryParseConditionRating(string? code, out InspectionConditionRating rating) =>
    TryParse(code, Ratings, out rating);

  public static bool TryParseDocumentKind(string? code, out InspectionDocumentKind kind) =>
    TryParse(code, DocumentKinds, out kind);

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
