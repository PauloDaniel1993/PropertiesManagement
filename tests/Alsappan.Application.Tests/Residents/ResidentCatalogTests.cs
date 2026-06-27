using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Residents;
using Alsappan.Domain.Residents;

namespace Alsappan.Application.Tests.Residents;

public sealed class ResidentCatalogTests
{
  [Fact]
  public void StatusOptionsDefaultToPortugueseLabels()
  {
    var statuses = ResidentCatalog.GetStatusOptions();

    Assert.Contains(statuses, status =>
      status.Code == "active" &&
      status.Label == "Ativo" &&
      status.Tone == StatusLabelTones.Success);
  }

  [Fact]
  public void PortalStatusOptionsCanRenderEnglishLabels()
  {
    var statuses = ResidentCatalog.GetPortalStatusOptions("en-US");

    Assert.Contains(statuses, status => status.Code == "not-invited" && status.Label == "Not invited");
  }

  [Fact]
  public void PrivacyFlagsRoundTripFromApiValues()
  {
    Assert.True(ResidentCatalog.TryParsePrivacyFlags(
      ["contact-data", "identification-data"],
      out var flags));

    Assert.True(flags.HasFlag(ResidentPrivacyOptions.ContactData));
    Assert.True(flags.HasFlag(ResidentPrivacyOptions.IdentificationData));
    Assert.Contains("contact-data", ResidentCatalog.ToPrivacyFlagCodes(flags));
  }
}
