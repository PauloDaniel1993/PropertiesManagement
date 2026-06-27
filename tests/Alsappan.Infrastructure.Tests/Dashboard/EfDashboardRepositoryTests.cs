using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Dashboard.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Dashboard;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Timeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Dashboard;

public sealed class EfDashboardRepositoryTests
{
  [Fact]
  public async Task GetOverviewAsyncAggregatesMetricsAndFiltersRecentActivity()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var now = new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);
    var today = DateOnly.FromDateTime(now.UtcDateTime);

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var relatedA = CreateRelatedRecords(organizationA, now);
      var relatedB = CreateRelatedRecords(organizationB, now);
      var availableProperty = CreateProperty(organizationA, "Apartamento Jardim", PropertyStatus.Available, now);

      setup.Properties.AddRange(relatedA.Property, availableProperty, relatedB.Property);
      setup.Residents.AddRange(relatedA.Resident, relatedB.Resident);
      setup.Contracts.AddRange(relatedA.Contract, relatedB.Contract);
      setup.PaymentCharges.AddRange(
        CreateCharge(organizationA, relatedA.Contract, "Aluguel Calabria", today.AddDays(-5), now),
        CreateCharge(organizationB, relatedB.Contract, "Aluguel outra org", today.AddDays(-5), now));
      setup.Occurrences.Add(CreateOccurrence(organizationA, relatedA, now));
      setup.Inspections.Add(CreateInspection(organizationA, relatedA, now.AddDays(2), now));
      setup.TimelineEntries.AddRange(
        CreateTimelineEntry(
          organizationA,
          "property.created",
          "property",
          relatedA.Property.Id.Value.ToString("D"),
          relatedA.Property.Name,
          now),
        CreateTimelineEntry(
          organizationA,
          "payment.received",
          "payment",
          EntityId.New().Value.ToString("D"),
          "Aluguel Calabria",
          now.AddMinutes(-1)),
        CreateTimelineEntry(
          organizationB,
          "property.created",
          "property",
          relatedB.Property.Id.Value.ToString("D"),
          relatedB.Property.Name,
          now));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfDashboardRepository(context);

    var snapshot = await repository.GetOverviewAsync(
      organizationA,
      today,
      now,
      new DashboardQueryPermissions(
        CanReadProperties: true,
        CanReadContracts: true,
        CanReadPayments: true,
        CanReadOccurrences: true,
        CanReadInspections: true,
        CanReadTimeline: true,
        ReadableActivityEntityTypes: new HashSet<string>(["property"], StringComparer.OrdinalIgnoreCase)),
      recentActivityLimit: 6);

    Assert.Equal(2, snapshot.Occupancy!.TotalProperties);
    Assert.Equal(1, snapshot.Occupancy.RentedProperties);
    Assert.Equal(1, snapshot.OverduePayments!.Count);
    Assert.Equal(1000m, snapshot.OverduePayments.TotalBalance);
    Assert.Equal(1, snapshot.ContractExpirations!.Count);
    Assert.Equal(1, snapshot.OpenOccurrences!.Count);
    Assert.Equal(1, snapshot.OpenOccurrences.UrgentCount);
    Assert.Equal(1, snapshot.PendingInspections!.Count);

    var activity = Assert.Single(snapshot.RecentActivity);
    Assert.Equal("property.created", activity.EventType);
    Assert.Equal("property", activity.SubjectEntityType);
  }

  private static AlsappanDbContext CreateContext(OrganizationId organizationId, string databaseName)
  {
    var options = new DbContextOptionsBuilder<AlsappanDbContext>()
      .UseInMemoryDatabase(databaseName)
      .ReplaceService<IModelCacheKeyFactory, AlsappanModelCacheKeyFactory>()
      .Options;

    return new AlsappanDbContext(
      options,
      new StaticActiveOrganizationContext(organizationId),
      new DatabaseOptions());
  }

  private static RelatedRecords CreateRelatedRecords(OrganizationId organizationId, DateTimeOffset now)
  {
    var property = CreateProperty(organizationId, "Casa Calabria", PropertyStatus.Rented, now);
    var resident = Resident.Create(
      EntityId.New(),
      organizationId,
      "Joao da Silva",
      null,
      "joao@example.com",
      "11999999999",
      null,
      "cpf",
      "12345678900",
      null,
      null,
      null,
      null,
      ResidentStatus.Active,
      ResidentPortalStatus.NotInvited,
      ResidentPrivacyOptions.None,
      null,
      null,
      now);
    var contract = LeaseContract.Create(
      EntityId.New(),
      organizationId,
      property.Id,
      resident.Id,
      [resident.Id],
      new DateOnly(2026, 6, 1),
      new DateOnly(2026, 7, 15),
      new Money(1000m, "BRL"),
      10,
      null,
      ContractAdjustmentIndex.Ipca,
      12,
      null,
      null,
      null,
      true,
      null,
      property.Name,
      resident.FullName,
      now);
    contract.Activate(now.AddMinutes(1), null);
    return new RelatedRecords(property, resident, contract);
  }

  private static RentalProperty CreateProperty(
    OrganizationId organizationId,
    string name,
    PropertyStatus status,
    DateTimeOffset now) =>
    RentalProperty.Create(
      EntityId.New(),
      organizationId,
      name,
      PropertyType.House,
      "Para testes",
      new Address("Rua Calabria", "82", null, "Vila Fazzione", "Sao Paulo", "SP", "00000-000"),
      status,
      new Money(1000m, "BRL"),
      1,
      "A1",
      null,
      now);

  private static PaymentCharge CreateCharge(
    OrganizationId organizationId,
    LeaseContract contract,
    string title,
    DateOnly dueDate,
    DateTimeOffset now) =>
    PaymentCharge.Create(
      EntityId.New(),
      organizationId,
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      null,
      title,
      "Mensalidade",
      dueDate,
      new Money(1000m, "BRL"),
      null,
      null,
      PaymentMethod.Pix,
      PaymentReconciliationStatus.Pending,
      null,
      "Contrato Casa Calabria",
      "Casa Calabria",
      "Joao da Silva",
      now);

  private static Occurrence CreateOccurrence(
    OrganizationId organizationId,
    RelatedRecords related,
    DateTimeOffset now) =>
    Occurrence.Create(
      EntityId.New(),
      organizationId,
      "Vazamento na cozinha",
      "Morador relatou vazamento recorrente.",
      OccurrenceType.Maintenance,
      OccurrencePriority.Urgent,
      related.Property.Id,
      related.Resident.Id,
      related.Contract.Id,
      UserId.New(),
      new DateOnly(2026, 6, 29),
      related.Property.Name,
      related.Resident.FullName,
      "Contrato Casa Calabria",
      "Bruno Operador",
      now,
      UserId.New());

  private static Inspection CreateInspection(
    OrganizationId organizationId,
    RelatedRecords related,
    DateTimeOffset scheduledAt,
    DateTimeOffset now) =>
    Inspection.Create(
      EntityId.New(),
      organizationId,
      InspectionType.MoveIn,
      related.Property.Id,
      related.Contract.Id,
      related.Resident.Id,
      scheduledAt,
      UserId.New(),
      "Ana Admin",
      "Vistoria de entrada",
      "Observacao",
      related.Property.Name,
      "Contrato Casa Calabria",
      related.Resident.FullName,
      now,
      null);

  private static TimelineEntry CreateTimelineEntry(
    OrganizationId organizationId,
    string eventType,
    string entityType,
    string entityId,
    string displayName,
    DateTimeOffset occurredAt) =>
    TimelineEntry.FromEnvelope(
      ModuleEventEnvelope.Create(
        organizationId,
        $"{entityType}s",
        eventType,
        occurredAt,
        EventActor.User(UserId.New(), "Ana Admin"),
        new EntityReference(entityType, entityId, displayName),
        ModuleEventConsumer.Timeline,
        new Dictionary<string, string>(StringComparer.Ordinal)),
      occurredAt.AddSeconds(1));

  private sealed record RelatedRecords(
    RentalProperty Property,
    Resident Resident,
    LeaseContract Contract);
}
