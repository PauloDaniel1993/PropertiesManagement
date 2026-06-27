using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Pets;
using Alsappan.Application.Pets.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Pets;

namespace Alsappan.Application.Tests.Pets;

public sealed class PetServiceTests
{
  [Fact]
  public async Task CreateAsyncCreatesPetFromContractAndWritesSideEffects()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var vaccinationDocumentId = EntityId.New();
    var authorizationDocumentId = EntityId.New();
    repository.Documents.Add(vaccinationDocumentId);
    repository.Documents.Add(authorizationDocumentId);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      audit,
      outbox,
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var result = await service.CreateAsync(CreateRequest(
      repository.Resident.ResidentId.Value,
      repository.Contract.ContractId.Value,
      vaccinationDocumentId.Value,
      authorizationDocumentId.Value));

    Assert.True(result.Succeeded);
    Assert.Single(repository.Pets);
    Assert.Equal(repository.Contract.PropertyId.Value, result.Value!.Property!.Id);
    Assert.Equal(repository.Resident.ResidentId.Value, result.Value.Resident.Id);
    Assert.Single(result.Value.VaccinationRecordDocuments);
    Assert.Single(result.Value.AuthorizationFormDocuments);
    Assert.Single(audit.Entries);
    Assert.Single(outbox.Envelopes);
    Assert.Equal("pet.created", outbox.Envelopes[0].EventName);
    Assert.Equal("pets", outbox.Envelopes[0].ModuleName);
  }

  [Fact]
  public async Task AuthorizeAsyncRequiresManagePermissionAndWritesEvent()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var pet = CreatePet(organizationId, repository.Contract);
    repository.Pets.Add(pet);
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      outbox,
      [PermissionCodes.Manage(PermissionModules.Pets)]);

    var result = await service.AuthorizeAsync(
      pet.Id.Value,
      new PetLifecycleRequestDto("Autorizado pela administracao"));

    Assert.True(result.Succeeded);
    Assert.Equal("authorized", result.Value!.AuthorizationStatus.Code);
    Assert.Equal("pet.authorized", Assert.Single(outbox.Envelopes).EventName);
  }

  [Fact]
  public async Task CreateAsyncRejectsPropertyOrResidentThatDoesNotBelongToContract()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var request = CreateRequest(
      repository.OtherResident.ResidentId.Value,
      repository.Contract.ContractId.Value,
      null,
      null) with
    {
      PropertyId = repository.OtherProperty.PropertyId.Value
    };

    var result = await service.CreateAsync(request);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("residentId", result.Errors!.Keys);
    Assert.Contains("propertyId", result.Errors.Keys);
    Assert.Empty(repository.Pets);
  }

  [Fact]
  public async Task CreateAsyncRejectsAuthorizationStatusWithoutManagePermission()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var request = CreateRequest(repository.Resident.ResidentId.Value, repository.Contract.ContractId.Value, null, null) with
    {
      AuthorizationStatus = "authorized"
    };

    var result = await service.CreateAsync(request);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
    Assert.Empty(repository.Pets);
  }

  [Fact]
  public async Task CreateAsyncRejectsAuthorizationNotesWithoutManagePermission()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var request = CreateRequest(repository.Resident.ResidentId.Value, repository.Contract.ContractId.Value, null, null) with
    {
      AuthorizationNotes = "Aprovacao inicial"
    };

    var result = await service.CreateAsync(request);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
    Assert.Empty(repository.Pets);
  }

  [Fact]
  public async Task UpdateAsyncRejectsAuthorizationStatusChangeWithoutManagePermission()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var pet = CreatePet(organizationId, repository.Contract);
    repository.Pets.Add(pet);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var result = await service.UpdateAsync(
      pet.Id.Value,
      UpdateRequest(repository.Resident.ResidentId.Value, repository.Contract.ContractId.Value) with
      {
        AuthorizationStatus = "authorized"
      });

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
    Assert.Equal(PetAuthorizationStatus.Pending, pet.AuthorizationStatus);
  }

  [Fact]
  public async Task UpdateAsyncRejectsAuthorizationNotesChangeWithoutManagePermission()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var pet = CreatePet(organizationId, repository.Contract);
    repository.Pets.Add(pet);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var result = await service.UpdateAsync(
      pet.Id.Value,
      UpdateRequest(repository.Resident.ResidentId.Value, repository.Contract.ContractId.Value) with
      {
        AuthorizationNotes = "Mudanca direta"
      });

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
    Assert.Null(pet.AuthorizationNotes);
  }

  [Fact]
  public async Task CreateAsyncRejectsInactiveContractContext()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var result = await service.CreateAsync(CreateRequest(
      repository.Resident.ResidentId.Value,
      repository.InactiveContract.ContractId.Value,
      null,
      null));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("contractId", result.Errors!.Keys);
    Assert.Empty(repository.Pets);
  }

  [Fact]
  public async Task UpdateAsyncReplacesPetDocumentLinksByKind()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakePetRepository(organizationId);
    var oldVaccinationDocumentId = EntityId.New();
    var oldAuthorizationDocumentId = EntityId.New();
    var newVaccinationDocumentId = EntityId.New();
    repository.Documents.AddRange([oldVaccinationDocumentId, oldAuthorizationDocumentId, newVaccinationDocumentId]);
    var pet = CreatePet(organizationId, repository.Contract);
    pet.LinkDocument(oldVaccinationDocumentId, PetDocumentKind.VaccinationRecord, "Old vaccination", DateTimeOffset.UtcNow, null);
    pet.LinkDocument(oldAuthorizationDocumentId, PetDocumentKind.AuthorizationForm, "Old authorization", DateTimeOffset.UtcNow, null);
    repository.Pets.Add(pet);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var result = await service.UpdateAsync(
      pet.Id.Value,
      UpdateRequest(repository.Resident.ResidentId.Value, repository.Contract.ContractId.Value) with
      {
        VaccinationRecordDocumentId = newVaccinationDocumentId.Value,
        AuthorizationFormDocumentId = null
      });

    Assert.True(result.Succeeded);
    Assert.Equal(newVaccinationDocumentId.Value, Assert.Single(result.Value!.VaccinationRecordDocuments).DocumentId);
    Assert.Empty(result.Value.AuthorizationFormDocuments);
    Assert.Contains(pet.DocumentLinks, link =>
      link.DocumentId == oldVaccinationDocumentId && link.Kind == PetDocumentKind.VaccinationRecord && link.IsDeleted);
    Assert.Contains(pet.DocumentLinks, link =>
      link.DocumentId == oldAuthorizationDocumentId && link.Kind == PetDocumentKind.AuthorizationForm && link.IsDeleted);
  }

  [Fact]
  public async Task ListAsyncRequiresReadPermission()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakePetRepository(OrganizationId.New()),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Pets)]);

    var result = await service.ListAsync(new PetListRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  [Fact]
  public async Task OptionsUseRequestedLocale()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakePetRepository(OrganizationId.New()),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Read(PermissionModules.Pets)]);

    var options = await service.GetOptionsAsync("en-US");

    Assert.Contains(options.Species, option => option.Code == "cat" && option.Label == "Cat");
    Assert.Contains(options.AuthorizationStatuses, option => option.Code == "authorized" && option.Label == "Authorized");
  }

  private static PetService CreateService(
    OrganizationId organizationId,
    FakePetRepository repository,
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

  private static PetCreateRequestDto CreateRequest(
    Guid residentId,
    Guid contractId,
    Guid? vaccinationDocumentId,
    Guid? authorizationDocumentId) =>
    new(
      residentId,
      null,
      contractId,
      "Luna",
      "cat",
      "SRD",
      "pending",
      null,
      vaccinationDocumentId,
      authorizationDocumentId,
      "Docil");

  private static PetUpdateRequestDto UpdateRequest(Guid residentId, Guid contractId) =>
    new(
      residentId,
      null,
      contractId,
      "Luna",
      "cat",
      "SRD",
      "pending",
      null,
      null,
      null,
      "Docil");

  private static Pet CreatePet(OrganizationId organizationId, PetContractSnapshot contract) =>
    Pet.Create(
      EntityId.New(),
      organizationId,
      contract.PrimaryResidentId,
      contract.PropertyId,
      contract.ContractId,
      "Luna",
      PetSpecies.Cat,
      "SRD",
      PetAuthorizationStatus.Pending,
      null,
      "Docil",
      contract.ResidentName,
      contract.PropertyName,
      contract.DisplayName,
      DateTimeOffset.UtcNow);

  private sealed class FakePetRepository : IPetRepository
  {
    public FakePetRepository(OrganizationId organizationId)
    {
      OrganizationId = organizationId;
      var residentId = EntityId.New();
      var propertyId = EntityId.New();
      Resident = new PetResidentSnapshot(residentId, "Joao da Silva");
      Property = new PetPropertySnapshot(propertyId, "Casa Calabria", "Rua Calabria, 82");
      Contract = new PetContractSnapshot(
        EntityId.New(),
        propertyId,
        residentId,
        [residentId],
        "Contrato Casa Calabria",
        "Casa Calabria",
        "Joao da Silva",
        true);
      InactiveContract = Contract with
      {
        ContractId = EntityId.New(),
        IsActive = false
      };
      OtherProperty = new PetPropertySnapshot(EntityId.New(), "Apartamento Centro", "Rua Central, 10");
      OtherResident = new PetResidentSnapshot(EntityId.New(), "Maria Souza");
    }

    public OrganizationId OrganizationId { get; }

    public PetResidentSnapshot Resident { get; }

    public PetResidentSnapshot OtherResident { get; }

    public PetPropertySnapshot Property { get; }

    public PetPropertySnapshot OtherProperty { get; }

    public PetContractSnapshot Contract { get; }

    public PetContractSnapshot InactiveContract { get; }

    public List<EntityId> Documents { get; } = [];

    public List<Pet> Pets { get; } = [];

    public Task<PagedResultDto<PetSnapshot>> ListAsync(
      PetListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = Pets
        .Where(pet => pet.OrganizationId == organizationId)
        .Select(BuildSnapshot)
        .ToArray();
      return Task.FromResult(new PagedResultDto<PetSnapshot>(rows, request.Page, request.PageSize, rows.Length));
    }

    public Task<Pet?> FindAsync(
      EntityId petId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Pets.FirstOrDefault(pet =>
        pet.Id == petId &&
        pet.OrganizationId == organizationId &&
        (includeArchived || !pet.IsDeleted)));
    }

    public async Task<PetSnapshot?> FindSnapshotAsync(
      EntityId petId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      var pet = await FindAsync(petId, organizationId, includeArchived, cancellationToken).ConfigureAwait(false);
      return pet is null ? null : BuildSnapshot(pet);
    }

    public Task<PetResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<PetResidentSnapshot?>(
        organizationId == OrganizationId
          ? residentId == Resident.ResidentId ? Resident : residentId == OtherResident.ResidentId ? OtherResident : null
          : null);
    }

    public Task<PetPropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<PetPropertySnapshot?>(
        organizationId == OrganizationId
          ? propertyId == Property.PropertyId ? Property : propertyId == OtherProperty.PropertyId ? OtherProperty : null
          : null);
    }

    public Task<PetContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<PetContractSnapshot?>(
        organizationId == OrganizationId
          ? contractId == Contract.ContractId ? Contract : contractId == InactiveContract.ContractId ? InactiveContract : null
          : null);
    }

    public Task<bool> DocumentExistsAsync(
      EntityId documentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(organizationId == OrganizationId && Documents.Contains(documentId));
    }

    public Task AddAsync(Pet pet, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Pets.Add(pet);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(Pet pet, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }

    private PetSnapshot BuildSnapshot(Pet pet) =>
      new(
        pet,
        pet.ResidentId == Resident.ResidentId ? Resident : OtherResident,
        pet.PropertyId == Property.PropertyId ? Property : null,
        pet.ContractId == Contract.ContractId ? Contract : null,
        pet.DocumentLinks
          .Where(link => !link.IsDeleted)
          .Select(link => new PetDocumentSnapshot(link.DocumentId, link.Kind, link.Label))
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
