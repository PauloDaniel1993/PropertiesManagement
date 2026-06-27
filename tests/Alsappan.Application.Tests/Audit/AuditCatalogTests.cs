using Alsappan.Application.Audit;

namespace Alsappan.Application.Tests.Audit;

public sealed class AuditCatalogTests
{
  [Fact]
  public void GetCategoryOptionsReturnsLocalizedLabels()
  {
    var ptBr = AuditCatalog.GetCategoryOptions("pt-BR");
    var enUs = AuditCatalog.GetCategoryOptions("en-US");

    Assert.Contains(ptBr, category => category.Code == "Security" && category.Label == "Seguranca");
    Assert.Contains(enUs, category => category.Code == "Security" && category.Label == "Security");
  }

  [Fact]
  public void GetActionLabelFallsBackToHumanizedAction()
  {
    Assert.Equal("Property created", AuditCatalog.GetActionLabel("property.created", "en-US"));
    Assert.Equal("unknown event", AuditCatalog.GetActionLabel("unknown.event", "en-US"));
  }
}
