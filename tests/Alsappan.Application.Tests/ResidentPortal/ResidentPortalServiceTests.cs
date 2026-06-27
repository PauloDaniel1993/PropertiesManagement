using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Files;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Contracts;
using Alsappan.Application.Contracts.Repositories;
using Alsappan.Application.Documents;
using Alsappan.Application.Documents.Repositories;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Inspections;
using Alsappan.Application.Inspections.Repositories;
using Alsappan.Application.Notifications;
using Alsappan.Application.Notifications.Repositories;
using Alsappan.Application.Occurrences;
using Alsappan.Application.Occurrences.Repositories;
using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Application.Payments.Repositories;
using Alsappan.Application.Properties;
using Alsappan.Application.Properties.Repositories;
using Alsappan.Application.ResidentPortal;
using Alsappan.Application.Residents;
using Alsappan.Application.Residents.Repositories;
using Alsappan.Application.Settings.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Domain.Settings;

namespace Alsappan.Application.Tests.ResidentPortal;

public sealed class ResidentPortalServiceTests
{
  [Fact]
  public async Task CreateOccurrenceAsyncAllowsPropertyLinkedToResidentContract()
  {
    var fixture = ResidentPortalFixture.Create();

    var result = await fixture.Service.CreateOccurrenceAsync(
        CreateOccurrenceRequest(propertyId: fixture.LinkedPropertyId.Value),
        "pt-BR",
        CancellationToken.None)
      .ConfigureAwait(true);

    Assert.True(result.Succeeded);
    Assert.NotNull(result.Value);
    Assert.Equal(fixture.LinkedPropertyId.Value, result.Value.Property!.Id);
    Assert.Single(fixture.Occurrences.Added);
  }

  [Fact]
  public async Task CreateOccurrenceAsyncRejectsPropertyNotLinkedToResident()
  {
    var fixture = ResidentPortalFixture.Create();

    var result = await fixture.Service.CreateOccurrenceAsync(
        CreateOccurrenceRequest(propertyId: fixture.ForeignPropertyId.Value),
        "pt-BR",
        CancellationToken.None)
      .ConfigureAwait(true);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains(nameof(ResidentPortalOccurrenceCreateRequestDto.PropertyId), result.Errors!.Keys);
    Assert.Empty(fixture.Occurrences.Added);
  }

  [Fact]
  public async Task CreateOccurrenceAsyncRejectsMismatchedContractAndProperty()
  {
    var fixture = ResidentPortalFixture.Create();

    var result = await fixture.Service.CreateOccurrenceAsync(
        CreateOccurrenceRequest(
          propertyId: fixture.ForeignPropertyId.Value,
          contractId: fixture.ContractId.Value),
        "pt-BR",
        CancellationToken.None)
      .ConfigureAwait(true);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains(nameof(ResidentPortalOccurrenceCreateRequestDto.PropertyId), result.Errors!.Keys);
    Assert.Empty(fixture.Occurrences.Added);
  }

  private static ResidentPortalOccurrenceCreateRequestDto CreateOccurrenceRequest(
    Guid? propertyId = null,
    Guid? contractId = null) =>
    new(
      "Vazamento na cozinha",
      "Morador relatou vazamento recorrente na pia da cozinha.",
      "maintenance",
      "medium",
      propertyId,
      contractId);

  private sealed record ResidentPortalFixture(
    ResidentPortalService Service,
    FakeOccurrenceRepository Occurrences,
    EntityId LinkedPropertyId,
    EntityId ForeignPropertyId,
    EntityId ContractId)
  {
    public static ResidentPortalFixture Create()
    {
      var now = new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero);
      var timeProvider = new FixedTimeProvider(now);
      var organizationId = OrganizationId.New();
      var userId = UserId.New();
      var residentId = EntityId.New();
      var linkedPropertyId = EntityId.New();
      var foreignPropertyId = EntityId.New();
      var contractId = EntityId.New();

      var resident = Resident.Create(
        residentId,
        organizationId,
        "Joao Morador",
        "Joao",
        "joao@example.com",
        "(11) 99999-0000",
        null,
        "cpf",
        "12345678909",
        null,
        null,
        null,
        null,
        ResidentStatus.Active,
        ResidentPortalStatus.Active,
        ResidentPrivacyOptions.None,
        null,
        userId,
        now,
        userId);
      var settings = OrganizationSettings.Create(EntityId.New(), organizationId, now, userId);
      var contract = LeaseContract.Create(
        contractId,
        organizationId,
        linkedPropertyId,
        residentId,
        [residentId],
        new DateOnly(2026, 1, 1),
        null,
        new Money(1_500m, "BRL"),
        10,
        null,
        ContractAdjustmentIndex.Ipca,
        12,
        null,
        null,
        null,
        false,
        null,
        "Apartamento vinculado",
        resident.FullName,
        now,
        userId);
      var contractSnapshot = new ContractSnapshot(
        contract,
        new ContractPropertySnapshot(linkedPropertyId, "Apartamento vinculado", "Sao Paulo/SP"),
        [new ContractResidentSnapshot(residentId, resident.FullName, true)]);
      var occurrenceContract = new OccurrenceContractSnapshot(
        contractId,
        linkedPropertyId,
        residentId,
        [residentId],
        "Contrato residencial",
        "Apartamento vinculado",
        resident.FullName);
      var occurrences = new FakeOccurrenceRepository(
        residentId,
        new Dictionary<EntityId, OccurrencePropertySnapshot>
        {
          [linkedPropertyId] = new(linkedPropertyId, "Apartamento vinculado", "Sao Paulo/SP"),
          [foreignPropertyId] = new(foreignPropertyId, "Apartamento de outro morador", "Santos/SP")
        },
        new Dictionary<EntityId, OccurrenceContractSnapshot>
        {
          [contractId] = occurrenceContract
        });

      var service = new ResidentPortalService(
        new FixedActiveOrganizationContextResolver(organizationId, userId),
        new FakeIdentityRepository(new ResidentAccountLink(EntityId.New(), organizationId, userId, residentId, now, userId)),
        new FakeResidentRepository(resident),
        new FakeSettingsRepository(settings),
        new ThrowingPropertyRepository(),
        new FakeContractRepository(contractSnapshot),
        new ThrowingPaymentRepository(),
        new ThrowingDocumentRepository(),
        occurrences,
        new ThrowingInspectionRepository(),
        new ThrowingNotificationRepository(),
        new ThrowingFileStorageProvider(),
        [],
        new RecordingAuditWriter(),
        new RecordingOutboxWriter(),
        timeProvider);

      return new ResidentPortalFixture(service, occurrences, linkedPropertyId, foreignPropertyId, contractId);
    }
  }

  private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
  {
    public override DateTimeOffset GetUtcNow() => timestamp;
  }

  private sealed class FixedActiveOrganizationContextResolver : IActiveOrganizationContextResolver
  {
    private readonly ActiveOrganizationContext context;

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId, UserId userId)
    {
      var membership = new OrganizationMembership(
        organizationId,
        [RoleCodes.ResidentUser],
        [],
        isActive: true);
      var user = new AuthenticatedUser(
        userId,
        "joao@example.com",
        "Joao Morador",
        [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(
      CancellationToken cancellationToken = default) =>
      ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
  }

  private sealed class FakeIdentityRepository(ResidentAccountLink link) : IIdentityRepository
  {
    public Task<ResidentAccountLink?> FindResidentAccountLinkAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<ResidentAccountLink?>(link.UserId == userId && link.OrganizationId == organizationId ? link : null);

    public Task<IdentityUser?> FindUserByIdAsync(
      UserId userId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IdentityUser?>();

    public Task<IdentityUser?> FindUserByEmailAsync(
      string email,
      UserAccountType accountType,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IdentityUser?>();

    public Task<bool> EmailExistsAsync(
      string email,
      UserAccountType accountType,
      UserId? excludingUserId = null,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<bool>();

    public Task<IReadOnlyList<IdentityMembership>> ListMembershipsAsync(
      UserId userId,
      bool includeInactive = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IReadOnlyList<IdentityMembership>>();

    public Task<IdentityMembership?> FindMembershipAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IdentityMembership?>();

    public Task<IReadOnlyList<IdentityOrganization>> ListOrganizationsAsync(
      IEnumerable<OrganizationId> organizationIds,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IReadOnlyList<IdentityOrganization>>();

    public Task<IdentityOrganization?> FindOrganizationAsync(
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IdentityOrganization?>();

    public Task<IdentityRole?> FindRoleByCodeAsync(
      string roleCode,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IdentityRole?>();

    public Task<ResidentAccountLink?> FindResidentAccountLinkByResidentAsync(
      EntityId residentId,
      OrganizationId organizationId,
      bool includeInactive = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<ResidentAccountLink?>();

    public Task AddUserAsync(
      IdentityUser user,
      IEnumerable<IdentityMembership> memberships,
      UserInvitation? invitation = null,
      ResidentAccountLink? residentAccountLink = null,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task AddMembershipAsync(
      IdentityMembership membership,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task AddUserInvitationAsync(
      UserInvitation invitation,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task AddResidentAccountLinkAsync(
      ResidentAccountLink link,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateUserAsync(
      IdentityUser user,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateMembershipAsync(
      IdentityMembership membership,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateResidentAccountLinkAsync(
      ResidentAccountLink link,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task<PagedResultDto<AdministratorListItemDto>> ListAdministratorsAsync(
      AdministratorListRequestDto filter,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PagedResultDto<AdministratorListItemDto>>();

    public Task<AdministratorDetailDto?> GetAdministratorAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<AdministratorDetailDto?>();
  }

  private sealed class FakeResidentRepository(Resident resident) : IResidentRepository
  {
    public Task<Resident?> FindAsync(
      EntityId residentId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<Resident?>(resident.Id == residentId && resident.OrganizationId == organizationId ? resident : null);

    public Task<PagedResultDto<Resident>> ListAsync(
      ResidentListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PagedResultDto<Resident>>();

    public Task<IReadOnlyList<Resident>> FindPotentialDuplicatesAsync(
      ResidentDuplicateWarningRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IReadOnlyList<Resident>>();

    public Task AddAsync(Resident resident, CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateAsync(Resident resident, CancellationToken cancellationToken = default) =>
      NotUsedAsync();
  }

  private sealed class FakeSettingsRepository(OrganizationSettings settings) : ISettingsRepository
  {
    public Task<OrganizationSettings> GetOrCreateSettingsAsync(
      OrganizationId organizationId,
      DateTimeOffset createdAt,
      UserId? createdByUserId = null,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(settings);

    public Task<IdentityOrganization?> FindOrganizationAsync(
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IdentityOrganization?>();

    public Task<IReadOnlyList<DomainCatalogSetting>> ListCatalogItemsAsync(
      OrganizationId organizationId,
      string? catalogType = null,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IReadOnlyList<DomainCatalogSetting>>();

    public Task EnsureCatalogDefaultsAsync(
      OrganizationId organizationId,
      DateTimeOffset createdAt,
      UserId? createdByUserId = null,
      string? catalogType = null,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task<UserLocalePreference?> FindUserLocalePreferenceAsync(
      OrganizationId organizationId,
      UserId userId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<UserLocalePreference?>();

    public void AddUserLocalePreference(UserLocalePreference preference) => NotUsed();

    public void AddCatalogItem(DomainCatalogSetting item) => NotUsed();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => NotUsedAsync();
  }

  private sealed class FakeContractRepository(ContractSnapshot snapshot) : IContractRepository
  {
    public Task<PagedResultDto<ContractSnapshot>> ListAsync(
      ContractListRequestDto request,
      OrganizationId organizationId,
      DateOnly today,
      CancellationToken cancellationToken = default)
    {
      IReadOnlyList<ContractSnapshot> items = snapshot.Contract.OrganizationId == organizationId ? [snapshot] : [];
      return Task.FromResult(new PagedResultDto<ContractSnapshot>(items, 1, Math.Max(1, request.PageSize), items.Count));
    }

    public Task<ContractSnapshot?> FindSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<ContractSnapshot?>();

    public Task<LeaseContract?> FindAsync(
      EntityId contractId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<LeaseContract?>();

    public Task<ContractPropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<ContractPropertySnapshot?>();

    public Task<IReadOnlyList<ContractResidentSnapshot>> GetResidentSnapshotsAsync(
      IReadOnlyCollection<EntityId> residentIds,
      EntityId primaryResidentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IReadOnlyList<ContractResidentSnapshot>>();

    public Task<bool> HasOverlappingActiveContractAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      DateOnly startDate,
      DateOnly? endDate,
      EntityId? ignoredContractId = null,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<bool>();

    public Task<bool> HasAnyActiveContractAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      EntityId? ignoredContractId = null,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<bool>();

    public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default) =>
      NotUsedAsync();
  }

  private sealed class FakeOccurrenceRepository(
    EntityId residentId,
    IReadOnlyDictionary<EntityId, OccurrencePropertySnapshot> properties,
    IReadOnlyDictionary<EntityId, OccurrenceContractSnapshot> contracts) : IOccurrenceRepository
  {
    public List<Occurrence> Added { get; } = [];

    public Task<OccurrenceContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(contracts.GetValueOrDefault(contractId));

    public Task<OccurrencePropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(properties.GetValueOrDefault(propertyId));

    public Task AddAsync(Occurrence occurrence, CancellationToken cancellationToken = default)
    {
      Added.Add(occurrence);
      return Task.CompletedTask;
    }

    public Task<OccurrenceSnapshot?> FindSnapshotAsync(
      EntityId occurrenceId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      var occurrence = Added.FirstOrDefault(item => item.Id == occurrenceId && item.OrganizationId == organizationId);
      if (occurrence is null)
      {
        return Task.FromResult<OccurrenceSnapshot?>(null);
      }

      var property = occurrence.PropertyId.HasValue
        ? properties.GetValueOrDefault(occurrence.PropertyId.Value)
        : null;
      var contract = occurrence.ContractId.HasValue
        ? contracts.GetValueOrDefault(occurrence.ContractId.Value)
        : null;
      var snapshot = new OccurrenceSnapshot(
        occurrence,
        property,
        new OccurrenceResidentSnapshot(residentId, "Joao Morador"),
        contract,
        null,
        [],
        [],
        [],
        [],
        []);

      return Task.FromResult<OccurrenceSnapshot?>(snapshot);
    }

    public Task<PagedResultDto<OccurrenceSnapshot>> ListAsync(
      OccurrenceListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PagedResultDto<OccurrenceSnapshot>>();

    public Task<Occurrence?> FindAsync(
      EntityId occurrenceId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<Occurrence?>();

    public Task<OccurrenceResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<OccurrenceResidentSnapshot?>();

    public Task<OccurrenceUserSnapshot?> GetAssignableUserSnapshotAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<OccurrenceUserSnapshot?>();

    public Task<bool> DocumentExistsAsync(
      EntityId documentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<bool>();

    public Task UpdateAsync(Occurrence occurrence, CancellationToken cancellationToken = default) =>
      NotUsedAsync();
  }

  private sealed class ThrowingPropertyRepository : IPropertyRepository
  {
    public Task<RentalProperty?> FindAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<RentalProperty?>();

    public Task<PagedResultDto<RentalProperty>> ListAsync(
      PropertyListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PagedResultDto<RentalProperty>>();

    public Task AddAsync(RentalProperty rentalProperty, CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateAsync(RentalProperty rentalProperty, CancellationToken cancellationToken = default) =>
      NotUsedAsync();
  }

  private sealed class ThrowingPaymentRepository : IPaymentRepository
  {
    public Task<PagedResultDto<PaymentSnapshot>> ListAsync(
      PaymentListRequestDto request,
      OrganizationId organizationId,
      DateOnly today,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PagedResultDto<PaymentSnapshot>>();

    public Task<PaymentCharge?> FindAsync(
      EntityId chargeId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PaymentCharge?>();

    public Task<PaymentSnapshot?> FindSnapshotAsync(
      EntityId chargeId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PaymentSnapshot?>();

    public Task<PaymentContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PaymentContractSnapshot?>();

    public Task<PaymentPropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PaymentPropertySnapshot?>();

    public Task<PaymentResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PaymentResidentSnapshot?>();

    public Task<bool> ReceiptDocumentExistsAsync(
      EntityId documentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<bool>();

    public Task<PaymentCharge?> FindByProviderReferenceAsync(
      string providerCode,
      string providerReference,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PaymentCharge?>();

    public Task AddAsync(PaymentCharge charge, CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateAsync(PaymentCharge charge, CancellationToken cancellationToken = default) =>
      NotUsedAsync();
  }

  private sealed class ThrowingDocumentRepository : IDocumentRepository
  {
    public Task<PagedResultDto<DocumentSnapshot>> ListAsync(
      DocumentListRequestDto request,
      OrganizationId organizationId,
      IReadOnlySet<string> readableLinkedEntityTypes,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PagedResultDto<DocumentSnapshot>>();

    public Task<DocumentRecord?> FindAsync(
      EntityId documentId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<DocumentRecord?>();

    public Task<DocumentSnapshot?> FindSnapshotAsync(
      EntityId documentId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<DocumentSnapshot?>();

    public Task AddAsync(DocumentRecord document, CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateAsync(DocumentRecord document, CancellationToken cancellationToken = default) =>
      NotUsedAsync();
  }

  private sealed class ThrowingInspectionRepository : IInspectionRepository
  {
    public Task<PagedResultDto<InspectionSnapshot>> ListAsync(
      InspectionListRequestDto request,
      OrganizationId organizationId,
      DateTimeOffset now,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PagedResultDto<InspectionSnapshot>>();

    public Task<Inspection?> FindAsync(
      EntityId inspectionId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<Inspection?>();

    public Task<InspectionSnapshot?> FindSnapshotAsync(
      EntityId inspectionId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<InspectionSnapshot?>();

    public Task<InspectionPropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<InspectionPropertySnapshot?>();

    public Task<InspectionContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<InspectionContractSnapshot?>();

    public Task<InspectionResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<InspectionResidentSnapshot?>();

    public Task<InspectionUserSnapshot?> GetAssigneeSnapshotAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<InspectionUserSnapshot?>();

    public Task<bool> DocumentExistsAsync(
      EntityId documentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<bool>();

    public Task AddAsync(Inspection inspection, CancellationToken cancellationToken = default) =>
      NotUsedAsync();

    public Task UpdateAsync(Inspection inspection, CancellationToken cancellationToken = default) =>
      NotUsedAsync();
  }

  private sealed class ThrowingNotificationRepository : INotificationRepository
  {
    public Task<PagedResultDto<NotificationRecordSnapshot>> ListAsync(
      NotificationListRequestDto request,
      OrganizationId organizationId,
      UserId userId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<PagedResultDto<NotificationRecordSnapshot>>();

    public Task<int> CountUnreadAsync(
      OrganizationId organizationId,
      UserId userId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<int>();

    public Task<NotificationRecordSnapshot?> MarkReadAsync(
      Guid id,
      OrganizationId organizationId,
      UserId userId,
      DateTimeOffset readAt,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<NotificationRecordSnapshot?>();

    public Task<int> MarkAllReadAsync(
      OrganizationId organizationId,
      UserId userId,
      DateTimeOffset readAt,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<int>();

    public Task<bool> ArchiveAsync(
      Guid id,
      OrganizationId organizationId,
      UserId userId,
      DateTimeOffset archivedAt,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<bool>();

    public Task<IReadOnlyList<NotificationPreferenceSnapshot>> ListPreferencesAsync(
      OrganizationId organizationId,
      UserId userId,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IReadOnlyList<NotificationPreferenceSnapshot>>();

    public Task<IReadOnlyList<NotificationPreferenceSnapshot>> SavePreferencesAsync(
      OrganizationId organizationId,
      UserId userId,
      IReadOnlyList<NotificationPreferenceWriteModel> preferences,
      DateTimeOffset savedAt,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<IReadOnlyList<NotificationPreferenceSnapshot>>();
  }

  private sealed class ThrowingFileStorageProvider : IFileStorageProvider
  {
    public Task<StoredFileDescriptor> SaveAsync(
      FileStorageRequest request,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<StoredFileDescriptor>();

    public Task<Stream> OpenReadAsync(
      OrganizationId organizationId,
      string storageKey,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync<Stream>();

    public Task DeleteAsync(
      OrganizationId organizationId,
      string storageKey,
      CancellationToken cancellationToken = default) =>
      NotUsedAsync();
  }

  private sealed class RecordingAuditWriter : IAuditWriter
  {
    public List<AuditEntryDraft> Entries { get; } = [];

    public Task WriteAsync(AuditEntryDraft entry, CancellationToken cancellationToken = default)
    {
      Entries.Add(entry);
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingOutboxWriter : IModuleEventOutboxWriter
  {
    public List<ModuleEventEnvelope> Envelopes { get; } = [];

    public Task EnqueueAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
      Envelopes.Add(envelope);
      return Task.CompletedTask;
    }
  }

  private static Task<T> NotUsedAsync<T>() =>
    throw new NotSupportedException("This dependency is not used by resident portal occurrence scoping tests.");

  private static Task NotUsedAsync() =>
    throw new NotSupportedException("This dependency is not used by resident portal occurrence scoping tests.");

  private static void NotUsed() =>
    throw new NotSupportedException("This dependency is not used by resident portal occurrence scoping tests.");
}
