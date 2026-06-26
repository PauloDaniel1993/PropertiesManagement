using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Infrastructure.Persistence;

namespace Alsappan.Infrastructure.Timeline;

public sealed class TimelineEntry : IOrganizationScoped
{
  private TimelineEntry()
  {
  }

  private TimelineEntry(ModuleEventEnvelope envelope, DateTimeOffset createdAt)
  {
    Id = Guid.NewGuid();
    EventId = envelope.EventId;
    OrganizationId = envelope.OrganizationId;
    ModuleName = envelope.ModuleName;
    EventName = envelope.EventName;
    OccurredAt = envelope.OccurredAt;
    ActorKind = envelope.Actor.ActorKind;
    ActorUserId = envelope.Actor.UserId;
    ActorDisplayName = envelope.Actor.DisplayName;
    SubjectEntityType = envelope.Subject.EntityType;
    SubjectEntityId = envelope.Subject.EntityId;
    SubjectDisplayName = envelope.Subject.DisplayName;
    RelatedEntitiesJson = InfrastructureJsonSerializer.Serialize(envelope.RelatedEntities);
    DataJson = InfrastructureJsonSerializer.Serialize(envelope.Data);
    CorrelationId = envelope.CorrelationId;
    CreatedAt = createdAt;
  }

  public Guid Id { get; private set; }

  public Guid EventId { get; private set; }

  public OrganizationId OrganizationId { get; private set; }

  public string ModuleName { get; private set; } = string.Empty;

  public string EventName { get; private set; } = string.Empty;

  public DateTimeOffset OccurredAt { get; private set; }

  public string ActorKind { get; private set; } = string.Empty;

  public UserId? ActorUserId { get; private set; }

  public string? ActorDisplayName { get; private set; }

  public string SubjectEntityType { get; private set; } = string.Empty;

  public string SubjectEntityId { get; private set; } = string.Empty;

  public string? SubjectDisplayName { get; private set; }

  public string RelatedEntitiesJson { get; private set; } = "[]";

  public string DataJson { get; private set; } = "{}";

  public string? CorrelationId { get; private set; }

  public DateTimeOffset CreatedAt { get; private set; }

  public static TimelineEntry FromEnvelope(
    ModuleEventEnvelope envelope,
    DateTimeOffset createdAt)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    if (createdAt == default)
    {
      throw new ArgumentException("Created timestamp is required.", nameof(createdAt));
    }

    return new TimelineEntry(envelope, createdAt);
  }
}
