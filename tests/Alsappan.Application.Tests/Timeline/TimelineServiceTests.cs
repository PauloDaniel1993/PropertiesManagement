using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Timeline;
using Alsappan.Application.Timeline.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Tests.Timeline;

public sealed class TimelineServiceTests
{
  [Fact]
  public async Task ListAsyncLocalizesEventsAndFiltersUnreadableSubjects()
  {
    var organizationId = OrganizationId.New();
    var property = CreateRecord(organizationId, "property.created", "property", "property-a", "Casa A");
    var resident = CreateRecord(organizationId, "resident.created", "resident", "resident-a", "Joao");
    var service = CreateService(
      organizationId,
      [property, resident],
      [
        PermissionCodes.Read(PermissionModules.Timeline),
        PermissionCodes.Read(PermissionModules.Properties)
      ]);

    var result = await service.ListAsync(new TimelineListRequestDto(Locale: "pt-BR"));

    Assert.True(result.Succeeded);
    var item = Assert.Single(result.Value!.Items);
    Assert.Equal("property.created", item.EventType);
    Assert.Equal("Imovel criado", item.EventTypeLabel);
    Assert.Equal("Imovel criado: Casa A", item.Display.Summary);
  }

  [Fact]
  public async Task ListEntityAsyncRequiresPermissionForTargetEntityType()
  {
    var organizationId = OrganizationId.New();
    var service = CreateService(
      organizationId,
      [],
      [PermissionCodes.Read(PermissionModules.Timeline)]);

    var result = await service.ListEntityAsync(
      "property",
      "property-a",
      new TimelineEntityListRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  [Fact]
  public async Task ListAsyncReturnsEmptyPageForDeniedEntityFilter()
  {
    var organizationId = OrganizationId.New();
    var property = CreateRecord(organizationId, "property.created", "property", "property-a", "Casa A");
    var service = CreateService(
      organizationId,
      [property],
      [
        PermissionCodes.Read(PermissionModules.Timeline),
        PermissionCodes.Read(PermissionModules.Residents)
      ]);

    var result = await service.ListAsync(new TimelineListRequestDto(EntityType: "property"));

    Assert.True(result.Succeeded);
    Assert.Empty(result.Value!.Items);
    Assert.Equal(0, result.Value.TotalItems);
  }

  private static TimelineService CreateService(
    OrganizationId organizationId,
    IReadOnlyList<TimelineEntryRecord> records,
    IEnumerable<string> permissions) =>
    new(
      new FakeTimelineRepository(records),
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId));

  private static TimelineEntryRecord CreateRecord(
    OrganizationId organizationId,
    string eventType,
    string entityType,
    string entityId,
    string displayName) =>
    new(
      Guid.NewGuid(),
      Guid.NewGuid(),
      $"{entityType}s",
      eventType,
      DateTimeOffset.UtcNow,
      new TimelineActorRecord("user", UserId.New(), "Ana Admin"),
      new EntityReference(entityType, entityId, displayName),
      [],
      new Dictionary<string, string>(StringComparer.Ordinal),
      null);

  private sealed class FakeTimelineRepository : ITimelineRepository
  {
    private readonly IReadOnlyList<TimelineEntryRecord> records;

    public FakeTimelineRepository(IReadOnlyList<TimelineEntryRecord> records)
    {
      this.records = records;
    }

    public Task<PagedResultDto<TimelineEntryRecord>> ListAsync(
      TimelineListRequestDto request,
      OrganizationId organizationId,
      IReadOnlySet<string>? readableSubjectEntityTypes,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var filtered = records
        .Where(record => readableSubjectEntityTypes is null ||
          readableSubjectEntityTypes.Contains(record.Subject.EntityType))
        .ToArray();

      return Task.FromResult(
        new PagedResultDto<TimelineEntryRecord>(filtered, request.Page, request.PageSize, filtered.Length));
    }

    public Task<PagedResultDto<TimelineEntryRecord>> ListEntityAsync(
      string entityType,
      string entityId,
      TimelineEntityListRequestDto request,
      OrganizationId organizationId,
      IReadOnlySet<string>? readableSubjectEntityTypes,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var filtered = records
        .Where(record => record.Subject.EntityType == entityType && record.Subject.EntityId == entityId)
        .ToArray();

      return Task.FromResult(
        new PagedResultDto<TimelineEntryRecord>(filtered, request.Page, request.PageSize, filtered.Length));
    }
  }

  private sealed class FixedPermissionService : IPermissionService
  {
    private readonly HashSet<string> permissions;

    public FixedPermissionService(IEnumerable<string> permissions)
    {
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
      var organizationId = OrganizationId.New();
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
}
