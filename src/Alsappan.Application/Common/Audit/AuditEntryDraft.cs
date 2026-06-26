using System.Collections.ObjectModel;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Audit;

public sealed record AuditEntryDraft
{
  public AuditEntryDraft(
    OrganizationId organizationId,
    string action,
    AuditEntryCategory category,
    EventActor actor,
    EntityReference target,
    DateTimeOffset occurredAt,
    IReadOnlyDictionary<string, string>? changedFields = null,
    IReadOnlyDictionary<string, string>? context = null,
    string? correlationId = null)
  {
    if (occurredAt == default)
    {
      throw new ArgumentException("Occurred timestamp is required.", nameof(occurredAt));
    }

    OrganizationId = organizationId;
    Action = Required(action, nameof(action));
    Category = category;
    Actor = actor ?? throw new ArgumentNullException(nameof(actor));
    Target = target ?? throw new ArgumentNullException(nameof(target));
    OccurredAt = occurredAt;
    ChangedFields = Copy(changedFields);
    Context = Copy(context);
    CorrelationId = Optional(correlationId);
  }

  public OrganizationId OrganizationId { get; }

  public string Action { get; }

  public AuditEntryCategory Category { get; }

  public EventActor Actor { get; }

  public EntityReference Target { get; }

  public DateTimeOffset OccurredAt { get; }

  public IReadOnlyDictionary<string, string> ChangedFields { get; }

  public IReadOnlyDictionary<string, string> Context { get; }

  public string? CorrelationId { get; }

  public static AuditEntryDraft FromModuleEvent(
    ModuleEventEnvelope envelope,
    AuditEntryCategory category = AuditEntryCategory.Mutation)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    var context = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["module"] = envelope.ModuleName,
      ["event"] = envelope.EventName,
    };

    if (!string.IsNullOrWhiteSpace(envelope.CausationId))
    {
      context["causationId"] = envelope.CausationId;
    }

    if (!string.IsNullOrWhiteSpace(envelope.Locale))
    {
      context["locale"] = envelope.Locale;
    }

    return new AuditEntryDraft(
      envelope.OrganizationId,
      envelope.EventName,
      category,
      envelope.Actor,
      envelope.Subject,
      envelope.OccurredAt,
      envelope.Data,
      context,
      envelope.CorrelationId);
  }

  private static ReadOnlyDictionary<string, string> Copy(IReadOnlyDictionary<string, string>? source)
  {
    if (source is null || source.Count == 0)
    {
      return ReadOnlyDictionary<string, string>.Empty;
    }

    var copy = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var (key, value) in source)
    {
      copy[Required(key, "metadata key")] = value;
    }

    return new ReadOnlyDictionary<string, string>(copy);
  }

  private static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    return value.Trim();
  }

  private static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
