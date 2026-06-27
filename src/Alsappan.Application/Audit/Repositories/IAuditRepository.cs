using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Audit.Repositories;

public interface IAuditRepository
{
  Task<PagedResultDto<AuditEntryRecord>> ListAsync(
    AuditListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<AuditEntryRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record AuditEntryRecord(
  Guid Id,
  string Action,
  string Category,
  DateTimeOffset OccurredAt,
  string ActorKind,
  Guid? ActorUserId,
  string? ActorDisplayName,
  string TargetEntityType,
  string TargetEntityId,
  string? TargetDisplayName,
  IReadOnlyDictionary<string, string> ChangedFields,
  IReadOnlyDictionary<string, string> Context,
  string? CorrelationId);
