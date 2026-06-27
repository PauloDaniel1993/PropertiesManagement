using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Contracts;
using Alsappan.Domain.Contracts;

namespace Alsappan.Application.Tests.Contracts;

public sealed class ContractCatalogTests
{
  [Fact]
  public void StatusOptionsDefaultToPortugueseLabels()
  {
    var statuses = ContractCatalog.GetStatusOptions();

    Assert.Contains(statuses, status =>
      status.Code == "active" &&
      status.Label == "Ativo" &&
      status.Tone == StatusLabelTones.Success);
    Assert.Contains(statuses, status =>
      status.Code == "ending-soon" &&
      status.Label == "A vencer" &&
      status.Tone == StatusLabelTones.Warning);
  }

  [Fact]
  public void StatusOptionsCanRenderEnglishLabels()
  {
    var statuses = ContractCatalog.GetStatusOptions("en-US");

    Assert.Contains(statuses, status => status.Code == "terminated" && status.Label == "Terminated");
  }

  [Fact]
  public void AdjustmentIndexCodesRoundTripFromApiValues()
  {
    Assert.True(ContractCatalog.TryParseAdjustmentIndex("igp-m", out var index));
    Assert.Equal(ContractAdjustmentIndex.Igpm, index);
    Assert.Equal("igpm", ContractCatalog.ToAdjustmentIndexCode(index));
  }
}
