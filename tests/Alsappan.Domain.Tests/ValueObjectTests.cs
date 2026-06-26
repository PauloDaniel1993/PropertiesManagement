using Alsappan.Domain.Common.ValueObjects;

namespace Alsappan.Domain.Tests;

public sealed class ValueObjectTests
{
  [Fact]
  public void MoneyNormalizesCurrencyAndAddsSameCurrency()
  {
    var rent = new Money(700m, "brl");
    var adjustment = new Money(50m, "BRL");

    var total = rent.Add(adjustment);

    Assert.Equal(750m, total.Amount);
    Assert.Equal("BRL", total.Currency);
  }

  [Fact]
  public void MoneyRejectsInvalidCurrency()
  {
    Assert.Throws<ArgumentException>(() => new Money(100m, "real"));
  }

  [Fact]
  public void DateRangeRejectsEndBeforeStart()
  {
    var start = new BusinessDate(new DateOnly(2026, 6, 1));
    var end = new BusinessDate(new DateOnly(2026, 5, 31));

    Assert.Throws<ArgumentException>(() => new DateRange(start, end));
  }

  [Fact]
  public void DateRangeContainsDatesInsideClosedRange()
  {
    var range = new DateRange(
      new BusinessDate(new DateOnly(2026, 6, 1)),
      new BusinessDate(new DateOnly(2026, 6, 30)));

    Assert.True(range.Contains(new BusinessDate(new DateOnly(2026, 6, 15))));
    Assert.False(range.Contains(new BusinessDate(new DateOnly(2026, 7, 1))));
  }

  [Fact]
  public void AddressTrimsAndNormalizesStateAndCountryCodes()
  {
    var address = new Address(
      " Rua Calabria ",
      " 82 ",
      null,
      " Vila Fazzione ",
      " Sao Paulo ",
      "sp",
      "05000-000");

    Assert.Equal("Rua Calabria", address.StreetLine);
    Assert.Equal("82", address.Number);
    Assert.Equal("SP", address.StateCode);
    Assert.Equal("BR", address.CountryCode);
  }

  [Fact]
  public void LocalizedCatalogLabelReturnsRequestedLabelOrDefault()
  {
    var label = new LocalizedCatalogLabel(
      "Property.Available",
      new Dictionary<string, string>
      {
        ["pt-BR"] = "Disponivel",
        ["en-US"] = "Available",
      });

    Assert.Equal("property.available", label.Code);
    Assert.Equal("Available", label.GetLabel("en-US"));
    Assert.Equal("Disponivel", label.GetLabel("es-ES"));
  }
}
