using System.Globalization;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Dashboard.Repositories;
using Alsappan.Application.Timeline;

namespace Alsappan.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
  private const int RecentActivityLimit = 6;
  private const int ContractExpirationWindowDays = 30;

  private readonly IDashboardRepository dashboardRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly TimeProvider timeProvider;

  public DashboardService(
    IDashboardRepository dashboardRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    TimeProvider? timeProvider = null)
  {
    this.dashboardRepository = dashboardRepository ?? throw new ArgumentNullException(nameof(dashboardRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<DashboardOverviewDto>> GetOverviewAsync(
    DashboardOverviewRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var dashboardPermission = await permissionService
      .AuthorizeAsync(PermissionCodes.Read(PermissionModules.Dashboard), cancellationToken)
      .ConfigureAwait(false);
    if (!dashboardPermission.IsGranted)
    {
      return ApplicationOperationResult<DashboardOverviewDto>.Failed(
        dashboardPermission.Failure == PermissionEvaluationFailure.Unauthenticated
          ? ApplicationOperationFailure.Unauthorized
          : ApplicationOperationFailure.Forbidden);
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    if (!context.Succeeded)
    {
      return ApplicationOperationResult<DashboardOverviewDto>.Failed(
        context.Failure == ActiveOrganizationResolutionFailure.Unauthenticated
          ? ApplicationOperationFailure.Unauthorized
          : ApplicationOperationFailure.Forbidden);
    }

    var effectivePermissions = await permissionService.GetEffectivePermissionsAsync(cancellationToken)
      .ConfigureAwait(false);
    var queryPermissions = BuildQueryPermissions(effectivePermissions);
    var now = timeProvider.GetUtcNow();
    var today = DateOnly.FromDateTime(now.UtcDateTime);
    var snapshot = await dashboardRepository.GetOverviewAsync(
        context.Context!.OrganizationId,
        today,
        now,
        queryPermissions,
        RecentActivityLimit,
        cancellationToken)
      .ConfigureAwait(false);
    var overview = MapOverview(snapshot, queryPermissions, request.Locale, now);

    return ApplicationOperationResult<DashboardOverviewDto>.Success(overview);
  }

  private static DashboardQueryPermissions BuildQueryPermissions(IReadOnlySet<string> permissions)
  {
    var canReadAll = HasPermission(permissions, PermissionCodes.Wildcard);
    var readableActivityTypes = canReadAll
      ? null
      : TimelineCatalog.KnownEntityTypes
        .Where(entityType =>
        {
          var permission = TimelineCatalog.GetReadPermissionForEntityType(entityType);
          return permission is not null && HasPermission(permissions, permission);
        })
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    return new DashboardQueryPermissions(
      canReadAll || HasPermission(permissions, PermissionCodes.Read(PermissionModules.Properties)),
      canReadAll || HasPermission(permissions, PermissionCodes.Read(PermissionModules.Contracts)),
      canReadAll || HasPermission(permissions, PermissionCodes.Read(PermissionModules.Payments)),
      canReadAll || HasPermission(permissions, PermissionCodes.Read(PermissionModules.Occurrences)),
      canReadAll || HasPermission(permissions, PermissionCodes.Read(PermissionModules.Inspections)),
      canReadAll || HasPermission(permissions, PermissionCodes.Read(PermissionModules.Timeline)),
      readableActivityTypes);
  }

  private static DashboardOverviewDto MapOverview(
    DashboardMetricsSnapshot snapshot,
    DashboardQueryPermissions permissions,
    string? locale,
    DateTimeOffset generatedAt)
  {
    var metrics = new[]
    {
      MapOccupancy(snapshot.Occupancy, permissions.CanReadProperties, locale),
      MapOverduePayments(snapshot.OverduePayments, permissions.CanReadPayments, locale),
      MapContractExpirations(snapshot.ContractExpirations, permissions.CanReadContracts, locale),
      MapOpenOccurrences(snapshot.OpenOccurrences, permissions.CanReadOccurrences, locale),
      MapPendingInspections(snapshot.PendingInspections, permissions.CanReadInspections, locale)
    };

    return new DashboardOverviewDto(
      generatedAt,
      metrics,
      MapRecentActivity(snapshot.RecentActivity, permissions.CanReadTimeline, locale),
      BuildEmptyState(snapshot, permissions, locale));
  }

  private static DashboardMetricDto MapOccupancy(
    DashboardOccupancySnapshot? snapshot,
    bool isVisible,
    string? locale)
  {
    if (!isVisible)
    {
      return HiddenMetric("occupancy", "/imoveis", locale);
    }

    var total = snapshot?.TotalProperties ?? 0;
    var rented = snapshot?.RentedProperties ?? 0;
    var rate = total == 0 ? 0m : Math.Round(rented * 100m / total, 1);

    return new DashboardMetricDto(
      "occupancy",
      Label("Ocupacao", "Occupancy", locale),
      Label("Imoveis alugados sobre o total ativo.", "Rented properties over active total.", locale),
      rate,
      FormatPercent(rate, locale),
      "%",
      total == 0 ? "neutral" : rate >= 80 ? "success" : "info",
      "/imoveis?status=rented",
      true,
      null,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["totalProperties"] = total.ToString(CultureInfo.InvariantCulture),
        ["rentedProperties"] = rented.ToString(CultureInfo.InvariantCulture)
      });
  }

  private static DashboardMetricDto MapOverduePayments(
    DashboardOverduePaymentSnapshot? snapshot,
    bool isVisible,
    string? locale)
  {
    if (!isVisible)
    {
      return HiddenMetric("overduePayments", "/pagamentos?overdueOnly=true", locale);
    }

    var count = snapshot?.Count ?? 0;
    var totalBalance = snapshot?.TotalBalance ?? 0m;
    var currency = snapshot?.Currency ?? "BRL";

    return new DashboardMetricDto(
      "overduePayments",
      Label("Pagamentos vencidos", "Overdue payments", locale),
      Label("Cobrancas pendentes com vencimento ultrapassado.", "Pending charges past due date.", locale),
      count,
      count.ToString(CultureInfo.InvariantCulture),
      null,
      count == 0 ? "success" : "danger",
      "/pagamentos?overdueOnly=true",
      true,
      null,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["totalBalance"] = totalBalance.ToString(CultureInfo.InvariantCulture),
        ["currency"] = currency
      });
  }

  private static DashboardMetricDto MapContractExpirations(
    DashboardContractExpirationSnapshot? snapshot,
    bool isVisible,
    string? locale)
  {
    if (!isVisible)
    {
      return HiddenMetric("contractExpirations", "/contratos?endingSoon=true", locale);
    }

    var count = snapshot?.Count ?? 0;
    var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
    if (snapshot?.NextExpiration is not null)
    {
      metadata["nextExpiration"] = snapshot.NextExpiration.Value.ToString("O", CultureInfo.InvariantCulture);
    }

    return new DashboardMetricDto(
      "contractExpirations",
      Label("Contratos a vencer", "Ending contracts", locale),
      Label(
        $"Contratos ativos que vencem nos proximos {ContractExpirationWindowDays} dias.",
        $"Active contracts ending in the next {ContractExpirationWindowDays} days.",
        locale),
      count,
      count.ToString(CultureInfo.InvariantCulture),
      null,
      count == 0 ? "success" : "warning",
      "/contratos?endingSoon=true",
      true,
      null,
      metadata);
  }

  private static DashboardMetricDto MapOpenOccurrences(
    DashboardOpenOccurrenceSnapshot? snapshot,
    bool isVisible,
    string? locale)
  {
    if (!isVisible)
    {
      return HiddenMetric("openOccurrences", "/ocorrencias?unresolvedOnly=true", locale);
    }

    var count = snapshot?.Count ?? 0;
    var urgentCount = snapshot?.UrgentCount ?? 0;

    return new DashboardMetricDto(
      "openOccurrences",
      Label("Ocorrencias abertas", "Open occurrences", locale),
      Label("Ocorrencias ainda nao resolvidas ou canceladas.", "Occurrences not yet resolved or cancelled.", locale),
      count,
      count.ToString(CultureInfo.InvariantCulture),
      null,
      urgentCount > 0 ? "danger" : count > 0 ? "warning" : "success",
      "/ocorrencias?unresolvedOnly=true",
      true,
      null,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["urgentCount"] = urgentCount.ToString(CultureInfo.InvariantCulture)
      });
  }

  private static DashboardMetricDto MapPendingInspections(
    DashboardPendingInspectionSnapshot? snapshot,
    bool isVisible,
    string? locale)
  {
    if (!isVisible)
    {
      return HiddenMetric("pendingInspections", "/vistorias?pendingOnly=true", locale);
    }

    var count = snapshot?.Count ?? 0;
    var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
    if (snapshot?.NextScheduledAt is not null)
    {
      metadata["nextScheduledAt"] = snapshot.NextScheduledAt.Value.ToString("O", CultureInfo.InvariantCulture);
    }

    return new DashboardMetricDto(
      "pendingInspections",
      Label("Vistorias pendentes", "Pending inspections", locale),
      Label("Vistorias agendadas ou em andamento ate os proximos 7 dias.", "Scheduled or in-progress inspections due within 7 days.", locale),
      count,
      count.ToString(CultureInfo.InvariantCulture),
      null,
      count == 0 ? "success" : "info",
      "/vistorias?pendingOnly=true",
      true,
      null,
      metadata);
  }

  private static DashboardRecentActivitySectionDto MapRecentActivity(
    IReadOnlyList<DashboardActivitySnapshot> activities,
    bool isVisible,
    string? locale)
  {
    if (!isVisible)
    {
      return new DashboardRecentActivitySectionDto(
        Label("Atividade recente", "Recent activity", locale),
        Label("Ultimos eventos operacionais.", "Latest operational events.", locale),
        false,
        "/timeline",
        HiddenReason(locale),
        []);
    }

    return new DashboardRecentActivitySectionDto(
      Label("Atividade recente", "Recent activity", locale),
      Label("Ultimos eventos operacionais.", "Latest operational events.", locale),
      true,
      "/timeline",
      null,
      activities.Select(activity =>
      {
        var subject = new Alsappan.Domain.Common.Events.EntityReference(
          activity.SubjectEntityType,
          activity.SubjectEntityId,
          activity.SubjectDisplayName);
        return new DashboardActivityDto(
          activity.Id,
          activity.EventType,
          TimelineCatalog.GetEventLabel(activity.EventType, locale),
          TimelineCatalog.BuildSummary(
            activity.EventType,
            activity.SubjectEntityType,
            activity.SubjectDisplayName,
            locale),
          activity.OccurredAt,
          TimelineCatalog.NormalizeEntityType(activity.SubjectEntityType),
          TimelineCatalog.GetEntityTypeLabel(activity.SubjectEntityType, locale),
          activity.SubjectDisplayName,
          TimelineCatalog.GetEntityRoute(subject.EntityType, subject.EntityId));
      }).ToArray());
  }

  private static DashboardEmptyStateDto? BuildEmptyState(
    DashboardMetricsSnapshot snapshot,
    DashboardQueryPermissions permissions,
    string? locale)
  {
    if (!permissions.CanReadProperties || (snapshot.Occupancy?.TotalProperties ?? 0) > 0)
    {
      return null;
    }

    return new DashboardEmptyStateDto(
      Label("Nenhum imovel cadastrado", "No properties registered", locale),
      Label("Cadastre o primeiro imovel para iniciar os indicadores operacionais.", "Register the first property to start operational indicators.", locale),
      Label("Novo imovel", "New property", locale),
      "/imoveis");
  }

  private static DashboardMetricDto HiddenMetric(string key, string route, string? locale) =>
    new(
      key,
      key switch
      {
        "occupancy" => Label("Ocupacao", "Occupancy", locale),
        "overduePayments" => Label("Pagamentos vencidos", "Overdue payments", locale),
        "contractExpirations" => Label("Contratos a vencer", "Ending contracts", locale),
        "openOccurrences" => Label("Ocorrencias abertas", "Open occurrences", locale),
        "pendingInspections" => Label("Vistorias pendentes", "Pending inspections", locale),
        _ => key
      },
      Label("Indicador oculto por permissao.", "Metric hidden by permission.", locale),
      null,
      null,
      null,
      "neutral",
      route,
      false,
      HiddenReason(locale),
      new Dictionary<string, string>(StringComparer.Ordinal));

  private static bool HasPermission(IReadOnlySet<string> permissions, string permissionCode) =>
    permissions.Contains(PermissionCodes.Wildcard) ||
    permissions.Contains(PermissionCodes.Normalize(permissionCode));

  private static string HiddenReason(string? locale) =>
    Label("Sem permissao para visualizar este indicador.", "No permission to view this metric.", locale);

  private static string Label(string ptBr, string enUs, string? locale) =>
    IsPortuguese(locale) ? ptBr : enUs;

  private static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) || locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);

  private static string FormatPercent(decimal value, string? locale)
  {
    var culture = IsPortuguese(locale) ? new CultureInfo("pt-BR") : new CultureInfo("en-US");
    return string.Concat(value.ToString("0.#", culture), "%");
  }
}
