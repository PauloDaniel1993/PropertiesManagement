using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Application.Payments.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Payments;

namespace Alsappan.Application.Tests.Payments;

public sealed class PaymentServiceTests
{
  [Fact]
  public async Task CreateAsyncCreatesChargeFromActiveContractAndWritesSideEffects()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePaymentRepository(organizationId);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      audit,
      outbox,
      [PermissionCodes.Write(PermissionModules.Payments)]);

    var result = await service.CreateAsync(CreateRequest(repository.Contract.ContractId.Value));

    Assert.True(result.Succeeded);
    Assert.Single(repository.Charges);
    Assert.Equal(repository.Contract.PropertyId.Value, result.Value!.Property!.Id);
    Assert.Equal(repository.Contract.PrimaryResidentId.Value, result.Value.Resident!.Id);
    Assert.Single(audit.Entries);
    Assert.Single(outbox.Envelopes);
    Assert.Equal("payment.created", outbox.Envelopes[0].EventName);
  }

  [Fact]
  public async Task CreateAsyncRejectsUtilityAccountOnlyLinkUntilUtilityModuleCanValidateTenancy()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePaymentRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Payments)]);

    var result = await service.CreateAsync(new PaymentCreateRequestDto(
      "Conta de consumo",
      "Energia",
      null,
      null,
      null,
      Guid.NewGuid(),
      new DateOnly(2026, 6, 30),
      new PaymentMoneyDto(300m, "BRL"),
      null,
      null,
      "pix",
      "pending",
      null));

    Assert.False(result.Succeeded);
    Assert.NotNull(result.Errors);
    Assert.Contains("utilityAccountId", result.Errors.Keys);
    Assert.Contains("contractId", result.Errors.Keys);
    Assert.Empty(repository.Charges);
  }

  [Fact]
  public async Task RecordTransactionAsyncSupportsPartialAndFullSettlement()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePaymentRepository(organizationId);
    var charge = CreateCharge(organizationId, repository.Contract);
    repository.Charges.Add(charge);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Manage(PermissionModules.Payments)]);

    var partial = await service.RecordTransactionAsync(charge.Id.Value, new PaymentTransactionRequestDto(
      new PaymentMoneyDto(400m, "BRL"),
      "pix",
      new DateOnly(2026, 6, 27),
      "PIX-1",
      null,
      null,
      null,
      "Parcial"));
    var full = await service.RecordTransactionAsync(charge.Id.Value, new PaymentTransactionRequestDto(
      new PaymentMoneyDto(600m, "BRL"),
      "pix",
      new DateOnly(2026, 6, 28),
      "PIX-2",
      null,
      null,
      null,
      "Final"));

    Assert.True(partial.Succeeded);
    Assert.Equal("partially-paid", partial.Value!.Status.Code);
    Assert.True(full.Succeeded);
    Assert.Equal("paid", full.Value!.Status.Code);
    Assert.Equal(0m, full.Value.Balance.Amount);
    Assert.Equal(2, charge.Transactions.Count);
  }

  [Fact]
  public async Task CreateInstructionAndProviderEventUseSameSettlementPath()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePaymentRepository(organizationId);
    var charge = CreateCharge(organizationId, repository.Contract);
    repository.Charges.Add(charge);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [
        PermissionCodes.Write(PermissionModules.Payments),
        PermissionCodes.Manage(PermissionModules.Payments)
      ]);

    var instruction = await service.CreateInstructionAsync(
      charge.Id.Value,
      new PaymentInstructionRequestDto("mock-pix"));
    var settlement = await service.ApplyProviderEventAsync(new PaymentProviderEventRequestDto(
      "mock-pix",
      instruction.Value!.ProviderReference,
      "settled",
      new DateOnly(2026, 6, 27)));

    Assert.True(instruction.Succeeded);
    Assert.Equal("mock-pix", charge.ProviderCode);
    Assert.Contains("mock-pix", charge.ProviderMetadataJson, StringComparison.Ordinal);
    Assert.True(settlement.Succeeded);
    Assert.Equal("paid", settlement.Value!.Status.Code);
    Assert.Single(charge.Transactions);
  }

  [Fact]
  public async Task ListAsyncRequiresReadPermission()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakePaymentRepository(OrganizationId.New()),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Payments)]);

    var result = await service.ListAsync(new PaymentListRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  private static PaymentService CreateService(
    OrganizationId organizationId,
    FakePaymentRepository repository,
    RecordingAuditWriter auditWriter,
    RecordingOutboxWriter outboxWriter,
    IEnumerable<string> permissions) =>
    new(
      repository,
      [new FakeInstructionProvider()],
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      auditWriter,
      outboxWriter,
      TimeProvider.System);

  private static PaymentCreateRequestDto CreateRequest(Guid contractId) =>
    new(
      "Aluguel junho",
      "Mensalidade",
      contractId,
      null,
      null,
      null,
      new DateOnly(2026, 6, 30),
      new PaymentMoneyDto(1000m, "BRL"),
      null,
      null,
      "pix",
      "pending",
      "Observacoes");

  private static PaymentCharge CreateCharge(
    OrganizationId organizationId,
    PaymentContractSnapshot contract) =>
    PaymentCharge.Create(
      EntityId.New(),
      organizationId,
      contract.ContractId,
      contract.PropertyId,
      contract.PrimaryResidentId,
      null,
      "Aluguel junho",
      "Mensalidade",
      new DateOnly(2026, 6, 30),
      new Money(1000m, "BRL"),
      null,
      null,
      PaymentMethod.Pix,
      PaymentReconciliationStatus.Pending,
      null,
      contract.DisplayName,
      contract.PropertyName,
      contract.ResidentName,
      DateTimeOffset.UtcNow);

  private sealed class FakePaymentRepository : IPaymentRepository
  {
    public FakePaymentRepository(OrganizationId organizationId)
    {
      Contract = new PaymentContractSnapshot(
        EntityId.New(),
        EntityId.New(),
        EntityId.New(),
        "Contrato Casa Calabria",
        "Casa Calabria",
        "Joao da Silva",
        true,
        new Money(1000m, "BRL"),
        10);
      Property = new PaymentPropertySnapshot(Contract.PropertyId, "Casa Calabria", "Rua Calabria, 82");
      Resident = new PaymentResidentSnapshot(Contract.PrimaryResidentId, "Joao da Silva");
      OrganizationId = organizationId;
    }

    public OrganizationId OrganizationId { get; }

    public PaymentContractSnapshot Contract { get; }

    public PaymentPropertySnapshot Property { get; }

    public PaymentResidentSnapshot Resident { get; }

    public List<PaymentCharge> Charges { get; } = [];

    public Task<PagedResultDto<PaymentSnapshot>> ListAsync(
      PaymentListRequestDto request,
      OrganizationId organizationId,
      DateOnly today,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = Charges
        .Where(charge => charge.OrganizationId == organizationId)
        .Select(BuildSnapshot)
        .ToArray();
      return Task.FromResult(new PagedResultDto<PaymentSnapshot>(rows, request.Page, request.PageSize, rows.Length));
    }

    public Task<PaymentCharge?> FindAsync(
      EntityId chargeId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Charges.FirstOrDefault(charge =>
        charge.Id == chargeId &&
        charge.OrganizationId == organizationId &&
        (includeArchived || !charge.IsDeleted)));
    }

    public async Task<PaymentSnapshot?> FindSnapshotAsync(
      EntityId chargeId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      var charge = await FindAsync(chargeId, organizationId, includeArchived, cancellationToken)
        .ConfigureAwait(false);
      return charge is null ? null : BuildSnapshot(charge);
    }

    public Task<PaymentContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<PaymentContractSnapshot?>(
        organizationId == OrganizationId && contractId == Contract.ContractId ? Contract : null);
    }

    public Task<PaymentPropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<PaymentPropertySnapshot?>(
        organizationId == OrganizationId && propertyId == Property.PropertyId ? Property : null);
    }

    public Task<PaymentResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<PaymentResidentSnapshot?>(
        organizationId == OrganizationId && residentId == Resident.ResidentId ? Resident : null);
    }

    public Task<bool> ReceiptDocumentExistsAsync(
      EntityId documentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(true);
    }

    public Task<PaymentCharge?> FindByProviderReferenceAsync(
      string providerCode,
      string providerReference,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Charges.FirstOrDefault(charge =>
        charge.OrganizationId == organizationId &&
        charge.ProviderCode == providerCode &&
        charge.ProviderReference == providerReference));
    }

    public Task AddAsync(PaymentCharge charge, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Charges.Add(charge);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(PaymentCharge charge, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }

    private PaymentSnapshot BuildSnapshot(PaymentCharge charge) =>
      new(
        charge,
        charge.ContractId == Contract.ContractId ? Contract : null,
        charge.PropertyId == Property.PropertyId ? Property : null,
        charge.ResidentId == Resident.ResidentId ? Resident : null,
        charge.ReceiptLinks.Select(link => new PaymentReceiptSnapshot(link.DocumentId, link.Label)).ToArray());
  }

  private sealed class FakeInstructionProvider : IPaymentInstructionProvider
  {
    public string ProviderCode => PaymentCatalog.MockPixProvider;

    public PaymentInstructionKind Kind => PaymentInstructionKind.Pix;

    public Task<PaymentInstructionDto> CreateInstructionAsync(
      PaymentProviderInstructionRequest request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(new PaymentInstructionDto(
        request.ChargeId,
        ProviderCode,
        $"mock-pix-{request.ChargeId:N}",
        "pix",
        "issued",
        request.Amount,
        request.DueDate,
        DateTimeOffset.UtcNow.AddHours(1),
        request.PayerSummary,
        null,
        null,
        $"pix://mock/{request.ChargeId:N}",
        $"PIX-{request.ChargeId:N}",
        null,
        null,
        new Dictionary<string, string>(StringComparer.Ordinal) { ["provider"] = ProviderCode }));
    }
  }

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
