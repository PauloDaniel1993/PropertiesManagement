using Alsappan.Application.Audit;
using Alsappan.Application.Audit.Repositories;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Tests.Audit;

public sealed class AuditServiceTests
{
  private static readonly OrganizationId OrganizationId = OrganizationId.New();

  [Fact]
  public async Task ListAsyncRequiresAuditReadPermission()
  {
    var service = new AuditService(
      new StubAuditRepository([]),
      new StubPermissionService(isGranted: false));

    var result = await service.ListAsync(new AuditListRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(Common.Results.ApplicationOperationFailure.Forbidden, result.Failure);
  }

  [Fact]
  public async Task ListAsyncReturnsLocalizedEntries()
  {
    var service = new AuditService(
      new StubAuditRepository(
      [
        new AuditEntryRecord(
          Guid.NewGuid(),
          "property.created",
          "Mutation",
          DateTimeOffset.UtcNow,
          "user",
          Guid.NewGuid(),
          "Paulo",
          "property",
          Guid.NewGuid().ToString("D"),
          "Calabria casa1",
          new Dictionary<string, string> { ["name"] = "Calabria casa1" },
          new Dictionary<string, string> { ["module"] = "properties" },
          "trace-a")
      ]),
      new StubPermissionService(isGranted: true));

    var result = await service.ListAsync(new AuditListRequestDto(Locale: "en-US"));

    Assert.True(result.Succeeded);
    var item = Assert.Single(result.Value!.Items);
    Assert.Equal("Property created", item.ActionLabel);
    Assert.Equal("Data", item.Category.Label);
    Assert.Equal("Paulo", item.ActorDisplayName);
  }

  private sealed class StubAuditRepository : IAuditRepository
  {
    private readonly IReadOnlyList<AuditEntryRecord> records;

    public StubAuditRepository(IReadOnlyList<AuditEntryRecord> records)
    {
      this.records = records;
    }

    public Task<PagedResultDto<AuditEntryRecord>> ListAsync(
      AuditListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(new PagedResultDto<AuditEntryRecord>(
        records,
        request.Page,
        request.PageSize,
        records.Count));
    }

    public Task<AuditEntryRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(records.FirstOrDefault(record => record.Id == id));
    }
  }

  private sealed class StubPermissionService : IPermissionService
  {
    private readonly bool isGranted;

    public StubPermissionService(bool isGranted)
    {
      this.isGranted = isGranted;
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
      return ValueTask.FromResult(isGranted
        ? PermissionEvaluationResult.Granted(requirement, OrganizationId)
        : PermissionEvaluationResult.Denied(
          requirement,
          PermissionEvaluationFailure.PermissionDenied,
          OrganizationId));
    }

    public ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      IReadOnlySet<string> permissions = isGranted
        ? new HashSet<string>([PermissionCodes.Read(PermissionModules.Audit)], StringComparer.Ordinal)
        : new HashSet<string>(StringComparer.Ordinal);

      return ValueTask.FromResult(permissions);
    }
  }
}
