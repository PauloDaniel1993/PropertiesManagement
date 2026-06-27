using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Infrastructure.Persistence;

namespace Alsappan.Infrastructure.Notifications;

public sealed class NotificationRecord : IOrganizationScoped, ISoftDeletable
{
  private NotificationRecord()
  {
  }

  private NotificationRecord(ModuleEventEnvelope envelope, DateTimeOffset createdAt, UserId? recipientUserId)
  {
    Id = Guid.NewGuid();
    EventId = envelope.EventId;
    OrganizationId = envelope.OrganizationId;
    RecipientUserId = recipientUserId;
    Category = envelope.ModuleName;
    EventName = envelope.EventName;
    Channel = "in-app";
    DeliveryStatus = "delivered";
    PayloadJson = InfrastructureJsonSerializer.Serialize(envelope.Data);
    SubjectEntityType = envelope.Subject.EntityType;
    SubjectEntityId = envelope.Subject.EntityId;
    SubjectDisplayName = envelope.Subject.DisplayName;
    OccurredAt = envelope.OccurredAt;
    CreatedAt = createdAt;
    CorrelationId = envelope.CorrelationId;
  }

  public Guid Id { get; private set; }

  public Guid EventId { get; private set; }

  public OrganizationId OrganizationId { get; private set; }

  public UserId? RecipientUserId { get; private set; }

  public string Category { get; private set; } = string.Empty;

  public string EventName { get; private set; } = string.Empty;

  public string Channel { get; private set; } = string.Empty;

  public string DeliveryStatus { get; private set; } = string.Empty;

  public string PayloadJson { get; private set; } = "{}";

  public string SubjectEntityType { get; private set; } = string.Empty;

  public string SubjectEntityId { get; private set; } = string.Empty;

  public string? SubjectDisplayName { get; private set; }

  public DateTimeOffset OccurredAt { get; private set; }

  public DateTimeOffset CreatedAt { get; private set; }

  public bool IsRead { get; private set; }

  public DateTimeOffset? ReadAt { get; private set; }

  public DateTimeOffset? DeletedAt { get; private set; }

  public UserId? DeletedByUserId { get; private set; }

  public bool IsDeleted => DeletedAt.HasValue;

  public string? CorrelationId { get; private set; }

  public static NotificationRecord FromEnvelope(
    ModuleEventEnvelope envelope,
    DateTimeOffset createdAt,
    UserId? recipientUserId = null)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    if (createdAt == default)
    {
      throw new ArgumentException("Created timestamp is required.", nameof(createdAt));
    }

    return new NotificationRecord(envelope, createdAt, recipientUserId);
  }

  public void MarkRead(DateTimeOffset readAt)
  {
    if (readAt == default)
    {
      throw new ArgumentException("Read timestamp is required.", nameof(readAt));
    }

    IsRead = true;
    ReadAt = readAt;
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId = null)
  {
    if (deletedAt == default)
    {
      throw new ArgumentException("Deleted timestamp is required.", nameof(deletedAt));
    }

    DeletedAt = deletedAt;
    DeletedByUserId = deletedByUserId;
  }
}
