using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Residents;
using Alsappan.Application.Residents.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Residents;

namespace Alsappan.Application.Tests.Residents;

public sealed class ResidentServiceTests
{
  [Fact]
  public async Task ListAsyncMasksSensitiveFieldsForReadOnlyPermission()
  {
    var organizationId = OrganizationId.New();
    var resident = CreateResident(organizationId, "Joao da Silva");
    var service = CreateService(
      organizationId,
      [resident],
      [PermissionCodes.Read(PermissionModules.Residents)]);

    var result = await service.ListAsync(new ResidentListRequestDto());

    Assert.True(result.Succeeded);
    var item = Assert.Single(result.Value!.Items);
    Assert.True(item.IsSensitiveMasked);
    Assert.Equal("****", item.Email);
    Assert.Equal("****", item.DocumentIdentifier);
    Assert.Equal("Dados protegidos", item.ContactSummary);
  }

  [Fact]
  public async Task ListAsyncKeepsSensitiveFieldsForWritePermission()
  {
    var organizationId = OrganizationId.New();
    var resident = CreateResident(organizationId, "Joao da Silva");
    var service = CreateService(
      organizationId,
      [resident],
      [
        PermissionCodes.Read(PermissionModules.Residents),
        PermissionCodes.Write(PermissionModules.Residents)
      ]);

    var result = await service.ListAsync(new ResidentListRequestDto(Locale: "en-US"));

    Assert.True(result.Succeeded);
    var item = Assert.Single(result.Value!.Items);
    Assert.False(item.IsSensitiveMasked);
    Assert.Equal("joao@example.com", item.Email);
    Assert.Equal("joao@example.com", item.ContactSummary);
    Assert.Equal("Active", item.Status.Label);
  }

  [Fact]
  public async Task DuplicateWarningsRequireWritePermission()
  {
    var organizationId = OrganizationId.New();
    var resident = CreateResident(organizationId, "Joao da Silva");
    var service = CreateService(
      organizationId,
      [resident],
      [PermissionCodes.Read(PermissionModules.Residents)]);

    var result = await service.GetDuplicateWarningsAsync(
      new ResidentDuplicateWarningRequestDto("JOAO@example.com", null, null));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  [Fact]
  public async Task DuplicateWarningsMatchNormalizedContactData()
  {
    var organizationId = OrganizationId.New();
    var resident = CreateResident(organizationId, "Joao da Silva");
    var service = CreateService(
      organizationId,
      [resident],
      [PermissionCodes.Write(PermissionModules.Residents)]);

    var result = await service.GetDuplicateWarningsAsync(
      new ResidentDuplicateWarningRequestDto("JOAO@EXAMPLE.COM", "(11) 99999-8888", null));

    Assert.True(result.Succeeded);
    Assert.Contains(result.Value!, warning => warning.Field == "email" && warning.ResidentId == resident.Id.Value);
    Assert.Contains(result.Value!, warning => warning.Field == "phone" && warning.ResidentId == resident.Id.Value);
  }

  [Fact]
  public async Task CreateArchiveAndRestoreWriteAuditAndOutboxEvents()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeResidentRepository([]);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      [],
      [
        PermissionCodes.Write(PermissionModules.Residents),
        PermissionCodes.Archive(PermissionModules.Residents)
      ],
      repository,
      audit,
      outbox);

    var create = await service.CreateAsync(CreateRequest("Maria Souza"));
    Assert.True(create.Succeeded);

    var archive = await service.ArchiveAsync(create.Value!.Id);
    Assert.True(archive.Succeeded);

    var restore = await service.RestoreAsync(create.Value.Id);
    Assert.True(restore.Succeeded);
    Assert.Equal("inactive", restore.Value!.Status.Code);
    Assert.Equal(3, audit.Entries.Count);
    Assert.Equal(3, outbox.Envelopes.Count);
  }

  private static ResidentService CreateService(
    OrganizationId organizationId,
    IReadOnlyList<Resident> residents,
    IEnumerable<string> permissions,
    FakeResidentRepository? repository = null,
    RecordingAuditWriter? auditWriter = null,
    RecordingOutboxWriter? outboxWriter = null) =>
    new(
      repository ?? new FakeResidentRepository(residents),
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      auditWriter ?? new RecordingAuditWriter(),
      outboxWriter ?? new RecordingOutboxWriter(),
      TimeProvider.System);

  private static ResidentCreateRequestDto CreateRequest(string fullName) =>
    new(
      fullName,
      null,
      "maria@example.com",
      "(11) 98888-7777",
      null,
      "CPF",
      "987.654.321-00",
      new DateOnly(1992, 7, 3),
      new ResidentEmergencyContactDto("Ana Souza", "Irma", "(11) 97777-6666"),
      "active",
      "not-invited",
      ["identification-data"],
      "Observacoes");

  private static Resident CreateResident(OrganizationId organizationId, string fullName) =>
    Resident.Create(
      EntityId.New(),
      organizationId,
      fullName,
      null,
      "joao@example.com",
      "(11) 99999-8888",
      null,
      "CPF",
      "123.456.789-00",
      new DateOnly(1985, 1, 20),
      "Maria",
      "Mae",
      "(11) 98888-7777",
      ResidentStatus.Active,
      ResidentPortalStatus.NotInvited,
      ResidentPrivacyOptions.IdentificationData,
      "Observacoes",
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

  private sealed class FakeResidentRepository : IResidentRepository
  {
    private readonly List<Resident> residents;

    public FakeResidentRepository(IReadOnlyList<Resident> residents)
    {
      this.residents = residents.ToList();
    }

    public Task<Resident?> FindAsync(
      EntityId residentId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(residents.FirstOrDefault(resident =>
        resident.Id == residentId &&
        resident.OrganizationId == organizationId &&
        (includeArchived || !resident.IsDeleted)));
    }

    public Task<PagedResultDto<Resident>> ListAsync(
      ResidentListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = residents.Where(resident => resident.OrganizationId == organizationId).ToArray();
      return Task.FromResult(new PagedResultDto<Resident>(rows, request.Page, request.PageSize, rows.Length));
    }

    public Task<IReadOnlyList<Resident>> FindPotentialDuplicatesAsync(
      ResidentDuplicateWarningRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<Resident>>(
        residents.Where(resident => resident.OrganizationId == organizationId).ToArray());
    }

    public Task AddAsync(Resident resident, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      residents.Add(resident);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(Resident resident, CancellationToken cancellationToken = default)
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
