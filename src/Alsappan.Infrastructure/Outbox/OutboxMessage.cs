using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Infrastructure.Persistence;

namespace Alsappan.Infrastructure.Outbox;

public sealed class OutboxMessage : IOrganizationScoped
{
  private OutboxMessage()
  {
  }

  private OutboxMessage(ModuleEventEnvelope envelope, DateTimeOffset enqueuedAt)
  {
    Id = Guid.NewGuid();
    EventId = envelope.EventId;
    OrganizationId = envelope.OrganizationId;
    ModuleName = envelope.ModuleName;
    EventName = envelope.EventName;
    OccurredAt = envelope.OccurredAt;
    ConsumerMask = (int)envelope.Consumers;
    PayloadJson = InfrastructureJsonSerializer.Serialize(envelope);
    CorrelationId = envelope.CorrelationId;
    CausationId = envelope.CausationId;
    EnqueuedAt = enqueuedAt;
  }

  public Guid Id { get; private set; }

  public Guid EventId { get; private set; }

  public OrganizationId OrganizationId { get; private set; }

  public string ModuleName { get; private set; } = string.Empty;

  public string EventName { get; private set; } = string.Empty;

  public DateTimeOffset OccurredAt { get; private set; }

  public int ConsumerMask { get; private set; }

  public string PayloadJson { get; private set; } = "{}";

  public string? CorrelationId { get; private set; }

  public string? CausationId { get; private set; }

  public DateTimeOffset EnqueuedAt { get; private set; }

  public DateTimeOffset? ProcessedAt { get; private set; }

  public int AttemptCount { get; private set; }

  public string? LastError { get; private set; }

  public bool IsProcessed => ProcessedAt.HasValue;

  public static OutboxMessage FromEnvelope(
    ModuleEventEnvelope envelope,
    DateTimeOffset enqueuedAt)
  {
    ArgumentNullException.ThrowIfNull(envelope);

    if (enqueuedAt == default)
    {
      throw new ArgumentException("Enqueued timestamp is required.", nameof(enqueuedAt));
    }

    return new OutboxMessage(envelope, enqueuedAt);
  }

  public ModuleEventEnvelope ToEnvelope() =>
    InfrastructureJsonSerializer.Deserialize<ModuleEventEnvelope>(PayloadJson);

  public void MarkProcessed(DateTimeOffset processedAt)
  {
    if (processedAt == default)
    {
      throw new ArgumentException("Processed timestamp is required.", nameof(processedAt));
    }

    ProcessedAt = processedAt;
    LastError = null;
  }

  public void MarkFailed(string error, DateTimeOffset failedAt)
  {
    if (failedAt == default)
    {
      throw new ArgumentException("Failed timestamp is required.", nameof(failedAt));
    }

    AttemptCount++;
    LastError = string.IsNullOrWhiteSpace(error)
      ? "Outbox dispatch failed."
      : error.Trim()[..Math.Min(error.Trim().Length, 1_000)];
  }
}
