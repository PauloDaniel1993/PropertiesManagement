using System.Collections.ObjectModel;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Domain.Common.Events;

public sealed record ModuleEventEnvelope
{
  public ModuleEventEnvelope(
    Guid eventId,
    OrganizationId organizationId,
    string moduleName,
    string eventName,
    DateTimeOffset occurredAt,
    EventActor actor,
    EntityReference subject,
    ModuleEventConsumer consumers,
    IReadOnlyDictionary<string, string>? data = null,
    IReadOnlyCollection<EntityReference>? relatedEntities = null,
    string? correlationId = null,
    string? causationId = null,
    string? locale = null)
  {
    if (eventId == Guid.Empty)
    {
      throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
    }

    if (occurredAt == default)
    {
      throw new ArgumentException("Occurred timestamp is required.", nameof(occurredAt));
    }

    if (consumers == ModuleEventConsumer.None)
    {
      throw new ArgumentException("At least one event consumer is required.", nameof(consumers));
    }

    EventId = eventId;
    OrganizationId = organizationId;
    ModuleName = Required(moduleName, nameof(moduleName));
    EventName = Required(eventName, nameof(eventName));
    OccurredAt = occurredAt;
    Actor = actor ?? throw new ArgumentNullException(nameof(actor));
    Subject = subject ?? throw new ArgumentNullException(nameof(subject));
    Consumers = consumers;
    Data = CopyData(data);
    RelatedEntities = (relatedEntities ?? []).ToArray();
    CorrelationId = Optional(correlationId);
    CausationId = Optional(causationId);
    Locale = Optional(locale);
  }

  public Guid EventId { get; }

  public OrganizationId OrganizationId { get; }

  public string ModuleName { get; }

  public string EventName { get; }

  public DateTimeOffset OccurredAt { get; }

  public EventActor Actor { get; }

  public EntityReference Subject { get; }

  public ModuleEventConsumer Consumers { get; }

  public IReadOnlyDictionary<string, string> Data { get; }

  public IReadOnlyCollection<EntityReference> RelatedEntities { get; }

  public string? CorrelationId { get; }

  public string? CausationId { get; }

  public string? Locale { get; }

  public static ModuleEventEnvelope Create(
    OrganizationId organizationId,
    string moduleName,
    string eventName,
    DateTimeOffset occurredAt,
    EventActor actor,
    EntityReference subject,
    ModuleEventConsumer consumers,
    IReadOnlyDictionary<string, string>? data = null,
    IReadOnlyCollection<EntityReference>? relatedEntities = null,
    string? correlationId = null,
    string? causationId = null,
    string? locale = null) =>
    new(
      Guid.NewGuid(),
      organizationId,
      moduleName,
      eventName,
      occurredAt,
      actor,
      subject,
      consumers,
      data,
      relatedEntities,
      correlationId,
      causationId,
      locale);

  public bool IsVisibleToResidentPortal =>
    Consumers.HasFlag(ModuleEventConsumer.ResidentPortal);

  private static ReadOnlyDictionary<string, string> CopyData(IReadOnlyDictionary<string, string>? data)
  {
    if (data is null || data.Count == 0)
    {
      return ReadOnlyDictionary<string, string>.Empty;
    }

    var copy = new Dictionary<string, string>(StringComparer.Ordinal);

    foreach (var (key, value) in data)
    {
      copy[Required(key, "data key")] = value;
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
