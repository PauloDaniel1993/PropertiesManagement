using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Search;
using Alsappan.Application.Search.Repositories;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Tests.Search;

public sealed class GlobalSearchServiceTests
{
  [Fact]
  public async Task SearchAsyncFiltersRepositoryQueryToReadableEntityTypes()
  {
    var organizationId = OrganizationId.New();
    var propertyId = Guid.NewGuid();
    var paymentId = Guid.NewGuid();
    var repository = new FakeGlobalSearchRepository(
      [
        new GlobalSearchResultRecord(
          "property",
          propertyId,
          "Casa Calabria",
          "Rua Calabria, 82",
          "name",
          $"/imoveis?propertyId={propertyId:D}",
          DateTimeOffset.UtcNow),
        new GlobalSearchResultRecord(
          "payment",
          paymentId,
          "Aluguel Calabria",
          "Vencimento 2026-06-20",
          "name",
          $"/pagamentos?paymentId={paymentId:D}",
          DateTimeOffset.UtcNow.AddMinutes(-1))
      ]);
    var service = CreateService(
      organizationId,
      repository,
      [PermissionCodes.Read(PermissionModules.Properties)]);

    var result = await service.SearchAsync(new GlobalSearchRequestDto(" casa ", Limit: 50, Locale: "en-US"));

    Assert.True(result.Succeeded);
    Assert.NotNull(repository.LastQuery);
    Assert.Equal("casa", repository.LastQuery.Query);
    Assert.Equal(10, repository.LastQuery.LimitPerEntity);
    Assert.Contains("property", repository.LastQuery.ReadableEntityTypes!);
    Assert.DoesNotContain("payment", repository.LastQuery.ReadableEntityTypes!);

    var group = Assert.Single(result.Value!.Groups);
    Assert.Equal("property", group.EntityType);
    Assert.Equal("Property", group.EntityTypeLabel);
    Assert.Equal("/imoveis?search=casa", group.Route);
    Assert.Equal("Name", Assert.Single(group.Results).MatchedField);
  }

  [Fact]
  public async Task SearchAsyncReturnsEmptyResultForShortQueriesWithoutRepositoryCall()
  {
    var repository = new FakeGlobalSearchRepository([]);
    var service = CreateService(
      OrganizationId.New(),
      repository,
      [PermissionCodes.Read(PermissionModules.Properties)]);

    var result = await service.SearchAsync(new GlobalSearchRequestDto("c"));

    Assert.True(result.Succeeded);
    Assert.Equal(0, result.Value!.TotalItems);
    Assert.Equal(0, repository.CallCount);
  }

  [Fact]
  public async Task GetContractAsyncDescribesSearchableEntitiesAndMatchedFields()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakeGlobalSearchRepository([]),
      []);

    var contract = await service.GetContractAsync("pt-BR");

    Assert.Contains(contract.EntityTypes, entity =>
      entity.EntityType == "payment" && entity.EntityTypeLabel == "Pagamento" && entity.ReadPermission == "payments.read");
    Assert.Contains(contract.MatchedFields, field => field.Value == "name" && field.Label == "Nome");
  }

  private static GlobalSearchService CreateService(
    OrganizationId organizationId,
    FakeGlobalSearchRepository repository,
    IEnumerable<string> permissions) =>
    new(
      repository,
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId));

  private sealed class FakeGlobalSearchRepository : IGlobalSearchRepository
  {
    private readonly IReadOnlyList<GlobalSearchResultRecord> records;

    public FakeGlobalSearchRepository(IReadOnlyList<GlobalSearchResultRecord> records)
    {
      this.records = records;
    }

    public int CallCount { get; private set; }

    public GlobalSearchQuery? LastQuery { get; private set; }

    public Task<IReadOnlyList<GlobalSearchResultRecord>> SearchAsync(
      OrganizationId organizationId,
      GlobalSearchQuery query,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      CallCount++;
      LastQuery = query;

      IReadOnlyList<GlobalSearchResultRecord> filtered = records
        .Where(record => query.ReadableEntityTypes is null ||
          query.ReadableEntityTypes.Contains(record.EntityType))
        .ToArray();
      return Task.FromResult(filtered);
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
      return ValueTask.FromResult(PermissionEvaluationResult.Granted(requirement, OrganizationId.New()));
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
