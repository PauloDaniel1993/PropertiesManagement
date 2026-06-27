using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Occurrences;

namespace Alsappan.Application.Occurrences;

public static class OccurrenceCatalog
{
  private static readonly Dictionary<OccurrenceType, (string Code, string Pt, string En, string Tone)> Types =
    new()
    {
      [OccurrenceType.Maintenance] = ("maintenance", "Manutencao", "Maintenance", StatusLabelTones.Info),
      [OccurrenceType.Complaint] = ("complaint", "Reclamacao", "Complaint", StatusLabelTones.Warning),
      [OccurrenceType.Incident] = ("incident", "Incidente", "Incident", StatusLabelTones.Danger),
      [OccurrenceType.Request] = ("request", "Solicitacao", "Request", StatusLabelTones.Neutral),
      [OccurrenceType.Security] = ("security", "Seguranca", "Security", StatusLabelTones.Danger),
      [OccurrenceType.Noise] = ("noise", "Barulho", "Noise", StatusLabelTones.Warning),
      [OccurrenceType.Other] = ("other", "Outra", "Other", StatusLabelTones.Neutral)
    };

  private static readonly Dictionary<OccurrencePriority, (string Code, string Pt, string En, string Tone)> Priorities =
    new()
    {
      [OccurrencePriority.Low] = ("low", "Baixa", "Low", StatusLabelTones.Neutral),
      [OccurrencePriority.Medium] = ("medium", "Media", "Medium", StatusLabelTones.Info),
      [OccurrencePriority.High] = ("high", "Alta", "High", StatusLabelTones.Warning),
      [OccurrencePriority.Urgent] = ("urgent", "Urgente", "Urgent", StatusLabelTones.Danger)
    };

  private static readonly Dictionary<OccurrenceStatus, (string Code, string Pt, string En, string Tone)> Statuses =
    new()
    {
      [OccurrenceStatus.Open] = ("open", "Aberta", "Open", StatusLabelTones.Warning),
      [OccurrenceStatus.Assigned] = ("assigned", "Atribuida", "Assigned", StatusLabelTones.Info),
      [OccurrenceStatus.InProgress] = ("in-progress", "Em andamento", "In progress", StatusLabelTones.Info),
      [OccurrenceStatus.Waiting] = ("waiting", "Aguardando", "Waiting", StatusLabelTones.Warning),
      [OccurrenceStatus.Resolved] = ("resolved", "Resolvida", "Resolved", StatusLabelTones.Success),
      [OccurrenceStatus.Cancelled] = ("cancelled", "Cancelada", "Cancelled", StatusLabelTones.Neutral),
      [OccurrenceStatus.Archived] = ("archived", "Arquivada", "Archived", "archived")
    };

  public static IReadOnlyList<StatusLabelDto> GetTypeOptions(string? locale = null) =>
    Types.OrderBy(type => type.Value.Code, StringComparer.Ordinal)
      .Select(type => ToTypeLabel(type.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetPriorityOptions(string? locale = null) =>
    Priorities.OrderBy(priority => priority.Value.Code, StringComparer.Ordinal)
      .Select(priority => ToPriorityLabel(priority.Key, locale))
      .ToArray();

  public static IReadOnlyList<StatusLabelDto> GetStatusOptions(string? locale = null) =>
    Statuses.OrderBy(status => status.Value.Code, StringComparer.Ordinal)
      .Select(status => ToStatusLabel(status.Key, locale))
      .ToArray();

  public static StatusLabelDto ToTypeLabel(OccurrenceType type, string? locale = null)
  {
    var labels = Types[type];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToPriorityLabel(OccurrencePriority priority, string? locale = null)
  {
    var labels = Priorities[priority];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static StatusLabelDto ToStatusLabel(OccurrenceStatus status, string? locale = null)
  {
    var labels = Statuses[status];
    return new StatusLabelDto(labels.Code, IsEnglish(locale) ? labels.En : labels.Pt, labels.Tone);
  }

  public static bool TryParseType(string? code, out OccurrenceType type) =>
    TryParse(code, Types, out type);

  public static bool TryParsePriority(string? code, out OccurrencePriority priority) =>
    TryParse(code, Priorities, out priority);

  public static bool TryParseStatus(string? code, out OccurrenceStatus status) =>
    TryParse(code, Statuses, out status);

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
