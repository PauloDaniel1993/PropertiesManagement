using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Contracts;
using Alsappan.Application.Contracts.Repositories;
using Alsappan.Application.Properties;
using Alsappan.Application.Properties.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Properties;

namespace Alsappan.Application.Tests.Contracts;

public sealed class ContractServiceTests
{
  [Fact]
  public async Task CreateAsyncCanCreateActiveContractWithMultipleResidents()
  {
    var organizationId = OrganizationId.New();
    var property = CreateProperty(organizationId);
    var primaryResidentId = EntityId.New();
    var secondaryResidentId = EntityId.New();
    var contractRepository = new FakeContractRepository(
      organizationId,
      property,
      [
        new ContractResidentSnapshot(primaryResidentId, "Joao da Silva", true),
        new ContractResidentSnapshot(secondaryResidentId, "Maria Souza", false)
      ]);
    var propertyRepository = new FakePropertyRepository([property]);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      contractRepository,
      propertyRepository,
      audit,
      outbox,
      [
        PermissionCodes.Write(PermissionModules.Contracts),
        PermissionCodes.Manage(PermissionModules.Contracts)
      ]);

    var result = await service.CreateAsync(CreateRequest(property.Id.Value, primaryResidentId.Value, [primaryResidentId.Value, secondaryResidentId.Value], "activate"));

    Assert.True(result.Succeeded);
    Assert.Equal("active", result.Value!.Status.Code);
    Assert.Equal(2, result.Value.Residents.Count);
    Assert.Equal(PropertyStatus.Rented, property.Status);
    Assert.Single(audit.Entries);
    Assert.Single(outbox.Envelopes);
    Assert.Equal("contract.created", outbox.Envelopes[0].EventName);
  }

  [Fact]
  public async Task CreateAsyncBlocksActivationWhenPropertyHasOverlappingActiveContract()
  {
    var organizationId = OrganizationId.New();
    var property = CreateProperty(organizationId);
    var primaryResidentId = EntityId.New();
    var existingResidentId = EntityId.New();
    var existing = CreateContract(organizationId, property.Id, existingResidentId, [existingResidentId]);
    existing.Activate(DateTimeOffset.UtcNow, null);
    var contractRepository = new FakeContractRepository(
      organizationId,
      property,
      [new ContractResidentSnapshot(primaryResidentId, "Joao da Silva", true)],
      [existing]);
    var service = CreateService(
      organizationId,
      contractRepository,
      new FakePropertyRepository([property]),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [
        PermissionCodes.Write(PermissionModules.Contracts),
        PermissionCodes.Manage(PermissionModules.Contracts)
      ]);

    var result = await service.CreateAsync(CreateRequest(property.Id.Value, primaryResidentId.Value, [primaryResidentId.Value], "activate"));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Conflict, result.Failure);
    Assert.DoesNotContain(contractRepository.Contracts, contract => contract.Id != existing.Id);
  }

  [Fact]
  public async Task ActivateAsyncUpdatesPropertyStatusAndWritesLifecycleEvents()
  {
    var organizationId = OrganizationId.New();
    var property = CreateProperty(organizationId);
    var primaryResidentId = EntityId.New();
    var contract = CreateContract(organizationId, property.Id, primaryResidentId, [primaryResidentId]);
    var contractRepository = new FakeContractRepository(
      organizationId,
      property,
      [new ContractResidentSnapshot(primaryResidentId, "Joao da Silva", true)],
      [contract]);
    var propertyRepository = new FakePropertyRepository([property]);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      contractRepository,
      propertyRepository,
      audit,
      outbox,
      [PermissionCodes.Manage(PermissionModules.Contracts)]);

    var result = await service.ActivateAsync(contract.Id.Value, new ContractLifecycleRequestDto());

    Assert.True(result.Succeeded);
    Assert.Equal("active", result.Value!.Status.Code);
    Assert.Equal(PropertyStatus.Rented, property.Status);
    Assert.Single(audit.Entries);
    Assert.Single(outbox.Envelopes);
    Assert.Equal("contract.activated", outbox.Envelopes[0].EventName);
  }

  [Fact]
  public async Task UpdateAsyncRejectsStaleConcurrencyToken()
  {
    var organizationId = OrganizationId.New();
    var property = CreateProperty(organizationId);
    var primaryResidentId = EntityId.New();
    var contract = CreateContract(organizationId, property.Id, primaryResidentId, [primaryResidentId]);
    var service = CreateService(
      organizationId,
      new FakeContractRepository(
        organizationId,
        property,
        [new ContractResidentSnapshot(primaryResidentId, "Joao da Silva", true)],
        [contract]),
      new FakePropertyRepository([property]),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Contracts)]);

    var result = await service.UpdateAsync(
      contract.Id.Value,
      UpdateRequest(primaryResidentId.Value, [primaryResidentId.Value], "stale-token"));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Conflict, result.Failure);
    Assert.Contains("concurrencyToken", result.Errors!.Keys);
  }

  [Fact]
  public async Task UpdateAsyncRejectsTerminatedContractsWithoutThrowing()
  {
    var organizationId = OrganizationId.New();
    var property = CreateProperty(organizationId);
    var primaryResidentId = EntityId.New();
    var contract = CreateContract(organizationId, property.Id, primaryResidentId, [primaryResidentId]);
    contract.Activate(DateTimeOffset.UtcNow, null);
    contract.Terminate(new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow.AddMinutes(1), null);
    var service = CreateService(
      organizationId,
      new FakeContractRepository(
        organizationId,
        property,
        [new ContractResidentSnapshot(primaryResidentId, "Joao da Silva", true)],
        [contract]),
      new FakePropertyRepository([property]),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Contracts)]);

    var result = await service.UpdateAsync(
      contract.Id.Value,
      UpdateRequest(primaryResidentId.Value, [primaryResidentId.Value], contract.ConcurrencyToken.Value));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Conflict, result.Failure);
    Assert.Contains("status", result.Errors!.Keys);
  }

  [Fact]
  public async Task TerminateAsyncKeepsPropertyRentedWhenAnotherActiveContractExists()
  {
    var organizationId = OrganizationId.New();
    var property = CreateProperty(organizationId);
    property.ChangeStatus(PropertyStatus.Rented, DateTimeOffset.UtcNow, null);
    var firstResidentId = EntityId.New();
    var secondResidentId = EntityId.New();
    var firstContract = CreateContract(organizationId, property.Id, firstResidentId, [firstResidentId]);
    var secondContract = CreateContract(organizationId, property.Id, secondResidentId, [secondResidentId]);
    firstContract.Activate(DateTimeOffset.UtcNow, null);
    secondContract.Activate(DateTimeOffset.UtcNow, null);
    var service = CreateService(
      organizationId,
      new FakeContractRepository(
        organizationId,
        property,
        [
          new ContractResidentSnapshot(firstResidentId, "Joao da Silva", true),
          new ContractResidentSnapshot(secondResidentId, "Maria Souza", true)
        ],
        [firstContract, secondContract]),
      new FakePropertyRepository([property]),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Manage(PermissionModules.Contracts)]);

    var result = await service.TerminateAsync(firstContract.Id.Value, new ContractLifecycleRequestDto(new DateOnly(2026, 9, 1)));

    Assert.True(result.Succeeded);
    Assert.Equal(PropertyStatus.Rented, property.Status);
  }

  [Fact]
  public async Task ListAsyncRequiresReadPermission()
  {
    var organizationId = OrganizationId.New();
    var property = CreateProperty(organizationId);
    var primaryResidentId = EntityId.New();
    var service = CreateService(
      organizationId,
      new FakeContractRepository(
        organizationId,
        property,
        [new ContractResidentSnapshot(primaryResidentId, "Joao da Silva", true)]),
      new FakePropertyRepository([property]),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Contracts)]);

    var result = await service.ListAsync(new ContractListRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  private static ContractService CreateService(
    OrganizationId organizationId,
    FakeContractRepository contractRepository,
    FakePropertyRepository propertyRepository,
    RecordingAuditWriter auditWriter,
    RecordingOutboxWriter outboxWriter,
    IEnumerable<string> permissions) =>
    new(
      contractRepository,
      propertyRepository,
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      auditWriter,
      outboxWriter,
      TimeProvider.System);

  private static ContractCreateRequestDto CreateRequest(
    Guid propertyId,
    Guid primaryResidentId,
    IReadOnlyList<Guid> residentIds,
    string lifecycleAction = "draft") =>
    new(
      propertyId,
      primaryResidentId,
      residentIds,
      new DateOnly(2026, 7, 1),
      new DateOnly(2027, 6, 30),
      new ContractMoneyDto(2500m, "BRL"),
      10,
      new ContractMoneyDto(2500m, "BRL"),
      "ipca",
      12,
      new DateOnly(2027, 7, 1),
      "Multa",
      "Desconto",
      true,
      "Observacoes",
      lifecycleAction);

  private static ContractUpdateRequestDto UpdateRequest(
    Guid primaryResidentId,
    IReadOnlyList<Guid> residentIds,
    string? concurrencyToken) =>
    new(
      primaryResidentId,
      residentIds,
      new DateOnly(2026, 7, 1),
      new DateOnly(2027, 6, 30),
      new ContractMoneyDto(2500m, "BRL"),
      10,
      new ContractMoneyDto(2500m, "BRL"),
      "ipca",
      12,
      new DateOnly(2027, 7, 1),
      "Multa",
      "Desconto",
      true,
      "Observacoes atualizadas",
      concurrencyToken);

  private static LeaseContract CreateContract(
    OrganizationId organizationId,
    EntityId propertyId,
    EntityId primaryResidentId,
    IReadOnlyList<EntityId> residentIds) =>
    LeaseContract.Create(
      EntityId.New(),
      organizationId,
      propertyId,
      primaryResidentId,
      residentIds,
      new DateOnly(2026, 7, 1),
      new DateOnly(2027, 6, 30),
      new Money(2500m, "BRL"),
      10,
      new Money(2500m, "BRL"),
      ContractAdjustmentIndex.Ipca,
      12,
      new DateOnly(2027, 7, 1),
      "Multa",
      "Desconto",
      true,
      "Observacoes",
      "Casa Calabria",
      "Joao da Silva",
      DateTimeOffset.UtcNow);

  private static RentalProperty CreateProperty(OrganizationId organizationId) =>
    RentalProperty.Create(
      EntityId.New(),
      organizationId,
      "Casa Calabria",
      PropertyType.House,
      "Para testes",
      new Address("Rua Calabria", "82", null, "Vila Fazzione", "Sao Paulo", "SP", "00000-000"),
      PropertyStatus.Available,
      new Money(2500m, "BRL"),
      1,
      "A1",
      null,
      DateTimeOffset.UtcNow);

  private sealed class FixedPermissionService : IPermissionService
  {
    private readonly HashSet<string> permissions;

    public FixedPermissionService(IEnumerable<string> permissions)
    {
      this.permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
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
          : PermissionEvaluationResult.Denied(requirement, PermissionEvaluationFailure.PermissionDenied, organizationId));
    }

    public ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default)
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
      var user = new AuthenticatedUser(UserId.New(), "admin@alsappan.local", "Paulo", [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(CancellationToken cancellationToken = default) =>
      ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
  }

  private sealed class FakeContractRepository : IContractRepository
  {
    private readonly OrganizationId organizationId;
    private readonly Dictionary<EntityId, ContractPropertySnapshot> properties = [];
    private readonly Dictionary<EntityId, ContractResidentSnapshot> residents = [];

    public FakeContractRepository(
      OrganizationId organizationId,
      RentalProperty property,
      IReadOnlyList<ContractResidentSnapshot> residents,
      IReadOnlyList<LeaseContract>? contracts = null)
    {
      this.organizationId = organizationId;
      properties[property.Id] = new ContractPropertySnapshot(
        property.Id,
        property.Name,
        $"{property.Address.StreetLine}, {property.Address.Number} - {property.Address.Neighborhood}, {property.Address.City}/{property.Address.StateCode}");

      foreach (var resident in residents)
      {
        this.residents[resident.ResidentId] = resident;
      }

      Contracts = contracts?.ToList() ?? [];
    }

    public List<LeaseContract> Contracts { get; }

    public Task<ContractSnapshot?> FindSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var contract = Contracts.FirstOrDefault(candidate =>
        candidate.Id == contractId &&
        candidate.OrganizationId == organizationId &&
        (includeArchived || !candidate.IsDeleted));
      return Task.FromResult(contract is null ? null : BuildSnapshot(contract));
    }

    public Task<LeaseContract?> FindAsync(
      EntityId contractId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Contracts.FirstOrDefault(contract =>
        contract.Id == contractId &&
        contract.OrganizationId == organizationId &&
        (includeArchived || !contract.IsDeleted)));
    }

    public Task<PagedResultDto<ContractSnapshot>> ListAsync(
      ContractListRequestDto request,
      OrganizationId organizationId,
      DateOnly today,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = Contracts
        .Where(contract => contract.OrganizationId == organizationId)
        .Select(BuildSnapshot)
        .ToArray();
      return Task.FromResult(new PagedResultDto<ContractSnapshot>(rows, request.Page, request.PageSize, rows.Length));
    }

    public Task<ContractPropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(
        organizationId == this.organizationId && properties.TryGetValue(propertyId, out var property)
          ? property
          : null);
    }

    public Task<IReadOnlyList<ContractResidentSnapshot>> GetResidentSnapshotsAsync(
      IReadOnlyCollection<EntityId> residentIds,
      EntityId primaryResidentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<ContractResidentSnapshot>>(
        residentIds
          .Where(residents.ContainsKey)
          .Select(id => residents[id] with { IsPrimary = id == primaryResidentId })
          .ToArray());
    }

    public Task<bool> HasOverlappingActiveContractAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      DateOnly startDate,
      DateOnly? endDate,
      EntityId? ignoredContractId = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var normalizedEnd = endDate ?? DateOnly.MaxValue;
      return Task.FromResult(Contracts.Any(contract =>
        contract.OrganizationId == organizationId &&
        contract.PropertyId == propertyId &&
        contract.Status == ContractStatus.Active &&
        !contract.IsDeleted &&
        (!ignoredContractId.HasValue || contract.Id != ignoredContractId.Value) &&
        contract.StartDate <= normalizedEnd &&
        (contract.EndDate is null || contract.EndDate >= startDate)));
    }

    public Task<bool> HasAnyActiveContractAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      EntityId? ignoredContractId = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Contracts.Any(contract =>
        contract.OrganizationId == organizationId &&
        contract.PropertyId == propertyId &&
        contract.Status == ContractStatus.Active &&
        !contract.IsDeleted &&
        (!ignoredContractId.HasValue || contract.Id != ignoredContractId.Value)));
    }

    public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Contracts.Add(leaseContract);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }

    private ContractSnapshot BuildSnapshot(LeaseContract contract) =>
      new(
        contract,
        properties[contract.PropertyId],
        contract.Residents
          .Select(resident => residents.TryGetValue(resident.ResidentId, out var snapshot)
            ? snapshot with { IsPrimary = resident.IsPrimary }
            : new ContractResidentSnapshot(resident.ResidentId, resident.ResidentId.Value.ToString("D"), resident.IsPrimary))
          .ToArray());
  }

  private sealed class FakePropertyRepository : IPropertyRepository
  {
    private readonly List<RentalProperty> properties;

    public FakePropertyRepository(IReadOnlyList<RentalProperty> properties)
    {
      this.properties = properties.ToList();
    }

    public Task<RentalProperty?> FindAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(properties.FirstOrDefault(property =>
        property.Id == propertyId &&
        property.OrganizationId == organizationId &&
        (includeArchived || !property.IsDeleted)));
    }

    public Task<PagedResultDto<RentalProperty>> ListAsync(
      PropertyListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = properties.Where(property => property.OrganizationId == organizationId).ToArray();
      return Task.FromResult(new PagedResultDto<RentalProperty>(rows, request.Page, request.PageSize, rows.Length));
    }

    public Task AddAsync(RentalProperty rentalProperty, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      properties.Add(rentalProperty);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(RentalProperty rentalProperty, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingAuditWriter : IAuditWriter
  {
    public List<AuditEntryDraft> Entries { get; } = [];

    public Task WriteAsync(AuditEntryDraft entry, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Entries.Add(entry);
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingOutboxWriter : IModuleEventOutboxWriter
  {
    public List<ModuleEventEnvelope> Envelopes { get; } = [];

    public Task EnqueueAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Envelopes.Add(envelope);
      return Task.CompletedTask;
    }
  }
}
