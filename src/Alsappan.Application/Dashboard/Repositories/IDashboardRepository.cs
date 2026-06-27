using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Dashboard.Repositories;

public sealed record DashboardQueryPermissions(
  bool CanReadProperties,
  bool CanReadContracts,
  bool CanReadPayments,
  bool CanReadOccurrences,
  bool CanReadInspections,
  bool CanReadTimeline,
  IReadOnlySet<string>? ReadableActivityEntityTypes);

public sealed record DashboardMetricsSnapshot(
  DashboardOccupancySnapshot? Occupancy,
  DashboardOverduePaymentSnapshot? OverduePayments,
  DashboardContractExpirationSnapshot? ContractExpirations,
  DashboardOpenOccurrenceSnapshot? OpenOccurrences,
  DashboardPendingInspectionSnapshot? PendingInspections,
  IReadOnlyList<DashboardActivitySnapshot> RecentActivity);

public sealed record DashboardOccupancySnapshot(
  int TotalProperties,
  int RentedProperties);

public sealed record DashboardOverduePaymentSnapshot(
  int Count,
  decimal TotalBalance,
  string Currency);

public sealed record DashboardContractExpirationSnapshot(
  int Count,
  DateOnly? NextExpiration);

public sealed record DashboardOpenOccurrenceSnapshot(
  int Count,
  int UrgentCount);

public sealed record DashboardPendingInspectionSnapshot(
  int Count,
  DateTimeOffset? NextScheduledAt);

public sealed record DashboardActivitySnapshot(
  Guid Id,
  string EventType,
  DateTimeOffset OccurredAt,
  string SubjectEntityType,
  string SubjectEntityId,
  string? SubjectDisplayName);

public interface IDashboardRepository
{
  Task<DashboardMetricsSnapshot> GetOverviewAsync(
    OrganizationId organizationId,
    DateOnly today,
    DateTimeOffset now,
    DashboardQueryPermissions permissions,
    int recentActivityLimit,
    CancellationToken cancellationToken = default);
}
