using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Dashboard;
using Alsappan.Application.Dashboard.Repositories;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Tests.Dashboard;

public sealed class DashboardServiceTests
{
  private static readonly DateTimeOffset FixedNow =
    new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task GetOverviewAsyncMasksUnreadableMetricsAndFiltersRecentActivity()
  {
    var organizationId = OrganizationId.New();
    var paymentActivityId = Guid.NewGuid();
    var repository = new FakeDashboardRepository(
      [
        new DashboardActivitySnapshot(
          Guid.NewGuid(),
          "property.created",
          FixedNow,
          "property",
          "property-a",
          "Casa A"),
        new DashboardActivitySnapshot(
          paymentActivityId,
          "payment.received",
          FixedNow.AddMinutes(-1),
          "payment",
          "payment-a",
          "Aluguel junho")
      ]);
    var service = CreateService(
      organizationId,
      repository,
      [
        PermissionCodes.Read(PermissionModules.Dashboard),
        PermissionCodes.Read(PermissionModules.Properties),
        PermissionCodes.Read(PermissionModules.Timeline)
      ]);

    var result = await service.GetOverviewAsync(new DashboardOverviewRequestDto("pt-BR"));

    Assert.True(result.Succeeded);
    Assert.NotNull(repository.LastPermissions);
    Assert.True(repository.LastPermissions.CanReadProperties);
    Assert.False(repository.LastPermissions.CanReadPayments);
    Assert.Contains("property", repository.LastPermissions.ReadableActivityEntityTypes!);
    Assert.DoesNotContain("payment", repository.LastPermissions.ReadableActivityEntityTypes!);

    var occupancy = Assert.Single(result.Value!.Metrics, metric => metric.Key == "occupancy");
    Assert.True(occupancy.IsVisible);
    Assert.Equal(75m, occupancy.Value);
    Assert.Equal("75%", occupancy.DisplayValue);

    var overdue = Assert.Single(result.Value.Metrics, metric => metric.Key == "overduePayments");
    Assert.False(overdue.IsVisible);
    Assert.Equal("Sem permissao para visualizar este indicador.", overdue.HiddenReason);

    var activity = Assert.Single(result.Value.RecentActivity.Items);
    Assert.Equal("property.created", activity.EventType);
    Assert.Equal("Imovel criado", activity.EventTypeLabel);
    Assert.Equal("Imovel criado: Casa A", activity.Summary);
    Assert.Equal("/imoveis?propertyId=property-a", activity.Route);
  }

  [Fact]
  public async Task GetOverviewAsyncRequiresDashboardReadPermission()
  {
    var repository = new FakeDashboardRepository([]);
    var service = CreateService(
      OrganizationId.New(),
      repository,
      [PermissionCodes.Read(PermissionModules.Properties)]);

    var result = await service.GetOverviewAsync(new DashboardOverviewRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
    Assert.Equal(0, repository.CallCount);
  }

  [Fact]
  public async Task GetOverviewAsyncReturnsLocalizedEmptyStateWhenNoPropertiesExist()
  {
    var repository = new FakeDashboardRepository([], totalProperties: 0, rentedProperties: 0);
    var service = CreateService(
      OrganizationId.New(),
      repository,
      [
        PermissionCodes.Read(PermissionModules.Dashboard),
        PermissionCodes.Read(PermissionModules.Properties)
      ]);

    var result = await service.GetOverviewAsync(new DashboardOverviewRequestDto("en-US"));

    Assert.True(result.Succeeded);
    Assert.NotNull(result.Value!.EmptyState);
    Assert.Equal("No properties registered", result.Value.EmptyState.Title);
    Assert.Equal("New property", result.Value.EmptyState.ActionLabel);
  }

  private static DashboardService CreateService(
    OrganizationId organizationId,
    FakeDashboardRepository repository,
    IEnumerable<string> permissions) =>
    new(
      repository,
      new FixedPermissionService(organizationId, permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      new FixedTimeProvider(FixedNow));

  private sealed class FakeDashboardRepository : IDashboardRepository
  {
    private readonly IReadOnlyList<DashboardActivitySnapshot> activities;
    private readonly int rentedProperties;
    private readonly int totalProperties;

    public FakeDashboardRepository(
      IReadOnlyList<DashboardActivitySnapshot> activities,
      int totalProperties = 4,
      int rentedProperties = 3)
    {
      this.activities = activities;
      this.totalProperties = totalProperties;
      this.rentedProperties = rentedProperties;
    }

    public int CallCount { get; private set; }

    public DashboardQueryPermissions? LastPermissions { get; private set; }

    public Task<DashboardMetricsSnapshot> GetOverviewAsync(
      OrganizationId organizationId,
      DateOnly today,
      DateTimeOffset now,
      DashboardQueryPermissions permissions,
      int recentActivityLimit,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      CallCount++;
      LastPermissions = permissions;

      var filteredActivities = permissions.CanReadTimeline
        ? activities
          .Where(activity => permissions.ReadableActivityEntityTypes is null ||
            permissions.ReadableActivityEntityTypes.Contains(activity.SubjectEntityType))
          .Take(recentActivityLimit)
          .ToArray()
        : [];

      return Task.FromResult(new DashboardMetricsSnapshot(
        permissions.CanReadProperties ? new DashboardOccupancySnapshot(totalProperties, rentedProperties) : null,
        permissions.CanReadPayments ? new DashboardOverduePaymentSnapshot(2, 1500m, "BRL") : null,
        permissions.CanReadContracts ? new DashboardContractExpirationSnapshot(1, today.AddDays(10)) : null,
        permissions.CanReadOccurrences ? new DashboardOpenOccurrenceSnapshot(4, 1) : null,
        permissions.CanReadInspections ? new DashboardPendingInspectionSnapshot(2, now.AddDays(1)) : null,
        filteredActivities));
    }
  }

  private sealed class FixedPermissionService : IPermissionService
  {
    private readonly OrganizationId organizationId;
    private readonly HashSet<string> permissions;

    public FixedPermissionService(OrganizationId organizationId, IEnumerable<string> permissions)
    {
      this.organizationId = organizationId;
      this.permissions = new HashSet<string>(
        permissions.Select(PermissionCodes.Normalize),
        StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      string permissionCode,
      CancellationToken cancellationToken = default) =>
      AuthorizeAsync(new PermissionRequirement(permissionCode), cancellationToken);

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      PermissionRequirement requirement,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var granted = permissions.Contains(PermissionCodes.Wildcard) ||
        permissions.Contains(requirement.PermissionCode);

      return ValueTask.FromResult(
        granted
          ? PermissionEvaluationResult.Granted(requirement, organizationId)
          : PermissionEvaluationResult.Denied(
            requirement,
            PermissionEvaluationFailure.PermissionDenied,
            organizationId));
    }

    public ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult<IReadOnlySet<string>>(permissions);
    }
  }

  private sealed class FixedActiveOrganizationContextResolver : IActiveOrganizationContextResolver
  {
    private readonly ActiveOrganizationContext context;

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId)
    {
      var membership = new OrganizationMembership(organizationId, permissionCodes: [PermissionCodes.Wildcard]);
      var user = new AuthenticatedUser(UserId.New(), "admin@alsappan.local", "Ana Admin", [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
    }
  }

  private sealed class FixedTimeProvider : TimeProvider
  {
    private readonly DateTimeOffset now;

    public FixedTimeProvider(DateTimeOffset now)
    {
      this.now = now;
    }

    public override DateTimeOffset GetUtcNow() => now;
  }
}
