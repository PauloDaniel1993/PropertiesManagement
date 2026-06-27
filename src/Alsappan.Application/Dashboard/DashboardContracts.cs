namespace Alsappan.Application.Dashboard;

public sealed record DashboardOverviewRequestDto(
  string? Locale = null);

public sealed record DashboardOverviewDto(
  DateTimeOffset GeneratedAt,
  IReadOnlyList<DashboardMetricDto> Metrics,
  DashboardRecentActivitySectionDto RecentActivity,
  DashboardEmptyStateDto? EmptyState);

public sealed record DashboardMetricDto(
  string Key,
  string Label,
  string Description,
  decimal? Value,
  string? DisplayValue,
  string? Unit,
  string Tone,
  string Route,
  bool IsVisible,
  string? HiddenReason,
  IReadOnlyDictionary<string, string> Metadata);

public sealed record DashboardRecentActivitySectionDto(
  string Label,
  string Description,
  bool IsVisible,
  string Route,
  string? HiddenReason,
  IReadOnlyList<DashboardActivityDto> Items);

public sealed record DashboardActivityDto(
  Guid Id,
  string EventType,
  string EventTypeLabel,
  string Summary,
  DateTimeOffset OccurredAt,
  string EntityType,
  string EntityTypeLabel,
  string? SubjectDisplayName,
  string? Route);

public sealed record DashboardEmptyStateDto(
  string Title,
  string Description,
  string ActionLabel,
  string Route);
