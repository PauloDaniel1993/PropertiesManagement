using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.UtilityAccounts;
using Alsappan.Application.UtilityAccounts.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.UtilityAccounts;

namespace Alsappan.Application.Tests.UtilityAccounts;

public sealed class UtilityAccountServiceTests
{
  [Fact]
  public async Task CreateAsyncCreatesUtilityAccountFromActiveContractAndWritesSideEffects()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeUtilityAccountRepository(organizationId);
    var billDocumentId = EntityId.New();
    repository.Documents.Add(billDocumentId);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      audit,
      outbox,
      [PermissionCodes.Write(PermissionModules.UtilityAccounts)]);

    var result = await service.CreateAsync(CreateRequest(repository.Contract.ContractId.Value, billDocumentId.Value));

    Assert.True(result.Succeeded);
    Assert.Single(repository.Accounts);
    Assert.Equal(repository.Contract.PropertyId.Value, result.Value!.Property!.Id);
    Assert.Equal(repository.Contract.PrimaryResidentId.Value, result.Value.Resident!.Id);
    Assert.Single(result.Value.BillDocuments);
    Assert.Single(audit.Entries);
    Assert.Single(outbox.Envelopes);
    Assert.Equal("utility-account.created", outbox.Envelopes[0].EventName);
  }

  [Fact]
  public async Task MarkPaidAsyncSettlesAccountAndLinksReceiptDocument()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeUtilityAccountRepository(organizationId);
    var receiptDocumentId = EntityId.New();
    repository.Documents.Add(receiptDocumentId);
    var account = CreateAccount(organizationId, repository.Contract);
    repository.Accounts.Add(account);
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      outbox,
      [PermissionCodes.Manage(PermissionModules.UtilityAccounts)]);

    var result = await service.MarkPaidAsync(
      account.Id.Value,
      new UtilityMarkPaidRequestDto(
        new UtilityMoneyDto(300m, "BRL"),
        new DateOnly(2026, 6, 27),
        "Pix",
        "PIX-1",
        receiptDocumentId.Value,
        "Pagamento recebido"));

    Assert.True(result.Succeeded);
    Assert.Equal("paid", result.Value!.Status.Code);
    Assert.Equal(0m, result.Value.Balance.Amount);
    Assert.Single(result.Value.ReceiptDocuments);
    Assert.Equal("utility-account.paid", Assert.Single(outbox.Envelopes).EventName);
  }

  [Fact]
  public async Task CreateAsyncRejectsUnknownBillDocument()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeUtilityAccountRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.UtilityAccounts)]);

    var result = await service.CreateAsync(CreateRequest(repository.Contract.ContractId.Value, Guid.NewGuid()));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("BillDocumentId", result.Errors!.Keys);
    Assert.Empty(repository.Accounts);
  }

  [Fact]
  public async Task ListAsyncRequiresReadPermission()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakeUtilityAccountRepository(OrganizationId.New()),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.UtilityAccounts)]);

    var result = await service.ListAsync(new UtilityAccountListRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  [Fact]
  public async Task OptionsUseRequestedLocale()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakeUtilityAccountRepository(OrganizationId.New()),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Read(PermissionModules.UtilityAccounts)]);

    var englishTypes = await service.GetTypeOptionsAsync("en-US");
    var portugueseResponsibilities = await service.GetResponsibilityOptionsAsync("pt-BR");

    Assert.Contains(englishTypes, option => option.Code == "electricity" && option.Label == "Electricity");
    Assert.Contains(portugueseResponsibilities, option => option.Code == "contract" && option.Label == "Contrato");
  }

  private static UtilityAccountService CreateService(
    OrganizationId organizationId,
    FakeUtilityAccountRepository repository,
    RecordingAuditWriter auditWriter,
    RecordingOutboxWriter outboxWriter,
    IEnumerable<string> permissions) =>
    new(
      repository,
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      auditWriter,
      outboxWriter,
      TimeProvider.System);

  private static UtilityAccountCreateRequestDto CreateRequest(Guid contractId, Guid billDocumentId) =>
    new(
      "Energia junho",
      "Conta de energia",
      "electricity",
      "contract",
      null,
      contractId,
      null,
      new DateOnly(2026, 6, 1),
      new DateOnly(2026, 6, 30),
      new DateOnly(2026, 6, 30),
      new UtilityMoneyDto(300m, "BRL"),
      billDocumentId,
      "Observacoes");

  private static UtilityAccount CreateAccount(
    OrganizationId organizationId,
    UtilityContractSnapshot contract) =>
    UtilityAccount.Create(
      EntityId.New(),
      organizationId,
      contract.PropertyId,
      contract.ContractId,
      contract.PrimaryResidentId,
      UtilityAccountType.Electricity,
      UtilityResponsibility.Contract,
      "Energia junho",
      "Conta de energia",
      new DateOnly(2026, 6, 1),
      new DateOnly(2026, 6, 30),
      new DateOnly(2026, 6, 30),
      new Money(300m, "BRL"),
      null,
      contract.PropertyName,
      contract.DisplayName,
      contract.ResidentName,
      DateTimeOffset.UtcNow);

  private sealed class FakeUtilityAccountRepository : IUtilityAccountRepository
  {
    public FakeUtilityAccountRepository(OrganizationId organizationId)
    {
      OrganizationId = organizationId;
      Contract = new UtilityContractSnapshot(
        EntityId.New(),
        EntityId.New(),
        EntityId.New(),
        "Contrato Casa Calabria",
        "Casa Calabria",
        "Joao da Silva",
        true);
      Property = new UtilityPropertySnapshot(Contract.PropertyId, "Casa Calabria", "Rua Calabria, 82");
      Resident = new UtilityResidentSnapshot(Contract.PrimaryResidentId, "Joao da Silva");
    }

    public OrganizationId OrganizationId { get; }

    public UtilityContractSnapshot Contract { get; }

    public UtilityPropertySnapshot Property { get; }

    public UtilityResidentSnapshot Resident { get; }

    public List<EntityId> Documents { get; } = [];

    public List<UtilityAccount> Accounts { get; } = [];

    public Task<PagedResultDto<UtilityAccountSnapshot>> ListAsync(
      UtilityAccountListRequestDto request,
      OrganizationId organizationId,
      DateOnly today,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = Accounts
        .Where(account => account.OrganizationId == organizationId)
        .Select(BuildSnapshot)
        .ToArray();
      return Task.FromResult(new PagedResultDto<UtilityAccountSnapshot>(
        rows,
        request.Page,
        request.PageSize,
        rows.Length));
    }

    public Task<UtilityAccount?> FindAsync(
      EntityId utilityAccountId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Accounts.FirstOrDefault(account =>
        account.Id == utilityAccountId &&
        account.OrganizationId == organizationId &&
        (includeArchived || !account.IsDeleted)));
    }

    public async Task<UtilityAccountSnapshot?> FindSnapshotAsync(
      EntityId utilityAccountId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      var account = await FindAsync(utilityAccountId, organizationId, includeArchived, cancellationToken)
        .ConfigureAwait(false);
      return account is null ? null : BuildSnapshot(account);
    }

    public Task<UtilityContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<UtilityContractSnapshot?>(
        organizationId == OrganizationId && contractId == Contract.ContractId ? Contract : null);
    }

    public Task<UtilityPropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<UtilityPropertySnapshot?>(
        organizationId == OrganizationId && propertyId == Property.PropertyId ? Property : null);
    }

    public Task<UtilityResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<UtilityResidentSnapshot?>(
        organizationId == OrganizationId && residentId == Resident.ResidentId ? Resident : null);
    }

    public Task<bool> DocumentExistsAsync(
      EntityId documentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(organizationId == OrganizationId && Documents.Contains(documentId));
    }

    public Task AddAsync(UtilityAccount utilityAccount, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Accounts.Add(utilityAccount);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(UtilityAccount utilityAccount, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }

    private UtilityAccountSnapshot BuildSnapshot(UtilityAccount account) =>
      new(
        account,
        account.PropertyId == Property.PropertyId ? Property : null,
        account.ContractId == Contract.ContractId ? Contract : null,
        account.ResidentId == Resident.ResidentId ? Resident : null,
        account.DocumentLinks
          .Select(link => new UtilityDocumentSnapshot(link.DocumentId, link.Kind, link.Label))
          .ToArray());
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
          : PermissionEvaluationResult.Denied(
            requirement,
            PermissionEvaluationFailure.PermissionDenied,
            organizationId));
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
