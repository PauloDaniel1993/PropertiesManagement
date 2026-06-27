using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;

namespace Alsappan.Domain.Tests.Contracts;

public sealed class LeaseContractTests
{
  [Fact]
  public void CreateStoresTermsAndResidentsWithPrimaryResponsible()
  {
    var primaryResidentId = EntityId.New();
    var secondaryResidentId = EntityId.New();
    var contract = CreateContract(primaryResidentId, [primaryResidentId, secondaryResidentId]);

    Assert.Equal(ContractStatus.Draft, contract.Status);
    Assert.Equal(primaryResidentId, contract.PrimaryResidentId);
    Assert.Equal(2, contract.Residents.Count);
    Assert.Contains(contract.Residents, resident => resident.ResidentId == primaryResidentId && resident.IsPrimary);
    Assert.Equal(10, contract.DueDay);
    Assert.Equal(ContractAdjustmentIndex.Ipca, contract.AdjustmentIndex);
    Assert.Contains("CASA CALABRIA", contract.SearchText, StringComparison.Ordinal);
  }

  [Fact]
  public void CreateTruncatesSearchTextToConfiguredStorageLength()
  {
    var primaryResidentId = EntityId.New();
    var longText = new string('a', 2000);
    var contract = LeaseContract.Create(
      EntityId.New(),
      OrganizationId.New(),
      EntityId.New(),
      primaryResidentId,
      [primaryResidentId],
      new DateOnly(2026, 6, 1),
      new DateOnly(2027, 5, 31),
      new Money(2500m, "BRL"),
      10,
      new Money(2500m, "BRL"),
      ContractAdjustmentIndex.Ipca,
      12,
      new DateOnly(2027, 6, 1),
      new string('b', 1000),
      new string('c', 1000),
      true,
      longText,
      longText,
      longText,
      DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

    Assert.Equal(ContractCode.MaxSearchTextLength, contract.SearchText.Length);
  }

  [Fact]
  public void CreateRequiresPrimaryResidentToBelongToResidentSet()
  {
    var primaryResidentId = EntityId.New();

    Assert.Throws<ArgumentException>(() => CreateContract(primaryResidentId, [EntityId.New()]));
  }

  [Fact]
  public void EffectiveStatusReflectsEndingSoonAndEndedDates()
  {
    var primaryResidentId = EntityId.New();
    var contract = CreateContract(
      primaryResidentId,
      [primaryResidentId],
      endDate: new DateOnly(2026, 7, 15));

    contract.Activate(DateTimeOffset.Parse("2026-06-27T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture), null);

    Assert.Equal(ContractStatus.Active, contract.GetEffectiveStatus(new DateOnly(2026, 6, 1)));
    Assert.Equal(ContractStatus.EndingSoon, contract.GetEffectiveStatus(new DateOnly(2026, 6, 27)));
    Assert.Equal(ContractStatus.Ended, contract.GetEffectiveStatus(new DateOnly(2026, 7, 16)));
  }

  [Fact]
  public void TerminateAndArchiveMoveContractThroughLifecycleStates()
  {
    var primaryResidentId = EntityId.New();
    var contract = CreateContract(primaryResidentId, [primaryResidentId]);
    var now = DateTimeOffset.Parse("2026-06-27T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    contract.Activate(now, null);
    contract.Terminate(new DateOnly(2026, 9, 1), now.AddMinutes(1), null);
    Assert.Equal(ContractStatus.Terminated, contract.Status);
    Assert.Equal(new DateOnly(2026, 9, 1), contract.EndDate);

    contract.Archive(now.AddMinutes(2), null);
    Assert.True(contract.IsDeleted);
    Assert.Equal(ContractStatus.Archived, contract.Status);

    contract.Restore(now.AddMinutes(3), null);
    Assert.False(contract.IsDeleted);
    Assert.Equal(ContractStatus.Draft, contract.Status);
  }

  private static LeaseContract CreateContract(
    EntityId primaryResidentId,
    IReadOnlyList<EntityId> residentIds,
    DateOnly? endDate = null) =>
    LeaseContract.Create(
      EntityId.New(),
      OrganizationId.New(),
      EntityId.New(),
      primaryResidentId,
      residentIds,
      new DateOnly(2026, 6, 1),
      endDate ?? new DateOnly(2027, 5, 31),
      new Money(2500m, "BRL"),
      10,
      new Money(2500m, "BRL"),
      ContractAdjustmentIndex.Ipca,
      12,
      new DateOnly(2027, 6, 1),
      "Multa contratual",
      "Desconto combinado",
      true,
      "Observacoes",
      "Casa Calabria",
      "Joao da Silva",
      DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
