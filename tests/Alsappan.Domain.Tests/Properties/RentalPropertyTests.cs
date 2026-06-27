using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Properties;

namespace Alsappan.Domain.Tests.Properties;

public sealed class RentalPropertyTests
{
  [Fact]
  public void CreateNormalizesSearchTextAndStoresStructuredGarageData()
  {
    var property = CreateProperty("Casa Calabria", "Vila Fazzione", 2);

    Assert.Equal("Casa Calabria", property.Name);
    Assert.Equal(PropertyStatus.Available, property.Status);
    Assert.True(property.HasGarage);
    Assert.Equal(2, property.GarageSpaceCount);
    Assert.Contains("CASA CALABRIA", property.SearchText, StringComparison.Ordinal);
    Assert.Contains("VILA FAZZIONE", property.SearchText, StringComparison.Ordinal);
  }

  [Fact]
  public void UpdateRefreshesSearchTextAndConcurrencyToken()
  {
    var property = CreateProperty("Casa Calabria", "Vila Fazzione", 1);
    var originalToken = property.ConcurrencyToken;
    var now = DateTimeOffset.Parse("2026-06-27T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    property.Update(
      "Apartamento Jardins",
      PropertyType.Apartment,
      "Novo cadastro",
      new Address("Rua Augusta", "100", null, "Jardins", "Sao Paulo", "SP", "01305-000"),
      PropertyStatus.Maintenance,
      new Money(2500m, "BRL"),
      0,
      null,
      "Sem vaga",
      now,
      null);

    Assert.NotEqual(originalToken, property.ConcurrencyToken);
    Assert.Equal(PropertyStatus.Maintenance, property.Status);
    Assert.False(property.HasGarage);
    Assert.Contains("JARDINS", property.SearchText, StringComparison.Ordinal);
  }

  [Fact]
  public void ArchiveAndRestoreMovePropertyThroughLifecycleStates()
  {
    var property = CreateProperty("Casa Calabria", "Vila Fazzione", 1);
    var now = DateTimeOffset.Parse("2026-06-27T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    property.Archive(now, null);
    Assert.True(property.IsDeleted);
    Assert.Equal(PropertyStatus.Archived, property.Status);

    property.Restore(now.AddMinutes(1), null);
    Assert.False(property.IsDeleted);
    Assert.Equal(PropertyStatus.Inactive, property.Status);
  }

  private static RentalProperty CreateProperty(
    string name,
    string neighborhood,
    int garageSpaceCount) =>
    RentalProperty.Create(
      EntityId.New(),
      OrganizationId.New(),
      name,
      PropertyType.House,
      "Para testes",
      new Address("Rua Calabria", "82", "Casa 1", neighborhood, "Sao Paulo", "SP", "00000-000"),
      PropertyStatus.Available,
      new Money(700m, "BRL"),
      garageSpaceCount,
      garageSpaceCount > 0 ? "A1" : null,
      "Observacoes",
      DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
