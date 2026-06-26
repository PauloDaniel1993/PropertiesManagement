using Alsappan.Application.Common.Audit;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Infrastructure.Persistence;

namespace Alsappan.Infrastructure.Audit;

public sealed class AuditLogEntry : IOrganizationScoped
{
  private AuditLogEntry()
  {
  }

  private AuditLogEntry(AuditEntryDraft draft)
  {
    Id = Guid.NewGuid();
    OrganizationId = draft.OrganizationId;
    Action = draft.Action;
    Category = draft.Category.ToString();
    OccurredAt = draft.OccurredAt;
    ActorKind = draft.Actor.ActorKind;
    ActorUserId = draft.Actor.UserId;
    ActorDisplayName = draft.Actor.DisplayName;
    TargetEntityType = draft.Target.EntityType;
    TargetEntityId = draft.Target.EntityId;
    TargetDisplayName = draft.Target.DisplayName;
    ChangedFieldsJson = InfrastructureJsonSerializer.Serialize(draft.ChangedFields);
    ContextJson = InfrastructureJsonSerializer.Serialize(draft.Context);
    CorrelationId = draft.CorrelationId;
  }

  public Guid Id { get; private set; }

  public OrganizationId OrganizationId { get; private set; }

  public string Action { get; private set; } = string.Empty;

  public string Category { get; private set; } = string.Empty;

  public DateTimeOffset OccurredAt { get; private set; }

  public string ActorKind { get; private set; } = string.Empty;

  public UserId? ActorUserId { get; private set; }

  public string? ActorDisplayName { get; private set; }

  public string TargetEntityType { get; private set; } = string.Empty;

  public string TargetEntityId { get; private set; } = string.Empty;

  public string? TargetDisplayName { get; private set; }

  public string ChangedFieldsJson { get; private set; } = "{}";

  public string ContextJson { get; private set; } = "{}";

  public string? CorrelationId { get; private set; }

  public static AuditLogEntry FromDraft(AuditEntryDraft draft)
  {
    ArgumentNullException.ThrowIfNull(draft);
    return new AuditLogEntry(draft);
  }
}
