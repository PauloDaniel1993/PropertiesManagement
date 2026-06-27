using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Properties;
using Alsappan.Domain.Properties;

namespace Alsappan.Application.Tests.Properties;

public sealed class PropertyCatalogTests
{
  [Fact]
  public void StatusOptionsDefaultToPortugueseLabels()
  {
    var statuses = PropertyCatalog.GetStatusOptions();

    Assert.Contains(statuses, status =>
      status.Code == "available" &&
      status.Label == "Disponivel" &&
      status.Tone == StatusLabelTones.Success);
  }

  [Fact]
  public void StatusOptionsCanRenderEnglishLabels()
  {
    var statuses = PropertyCatalog.GetStatusOptions("en-US");

    Assert.Contains(statuses, status => status.Code == "rented" && status.Label == "Rented");
  }

  [Fact]
  public void TypeCodesRoundTripFromApiValues()
  {
    Assert.True(PropertyCatalog.TryParseType("commercial-room", out var type));
    Assert.Equal(PropertyType.CommercialRoom, type);
    Assert.Equal("commercial-room", PropertyCatalog.ToTypeCode(type));
  }
}
