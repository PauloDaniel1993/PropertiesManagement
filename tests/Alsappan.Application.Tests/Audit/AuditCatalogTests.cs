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

  [Theory]
  [InlineData("auth.login.succeeded", "Login realizado", "Login completed")]
  [InlineData("auth.token.refreshed", "Sessao renovada", "Session refreshed")]
  [InlineData("administrators.invited", "Administrador convidado", "Administrator invited")]
  [InlineData("administrators.role.changed", "Permissoes do administrador alteradas", "Administrator access changed")]
  [InlineData("property.status.rented", "Imovel alugado", "Property rented")]
  public void GetActionLabelReturnsLabelsForEmittedAuditActions(
    string action,
    string ptBr,
    string enUs)
  {
    Assert.Equal(ptBr, AuditCatalog.GetActionLabel(action, "pt-BR"));
    Assert.Equal(enUs, AuditCatalog.GetActionLabel(action, "en-US"));
  }
}
