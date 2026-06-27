using Alsappan.Api.Contracts;
using Alsappan.Api.Errors;
using Alsappan.Application.Common.Validation;
using Microsoft.AspNetCore.Http;

namespace Alsappan.Api.Tests;

public sealed class ProblemDetailsContractTests
{
  [Fact]
  public void FactoryUsesBrazilianPortugueseByDefault()
  {
    var httpContext = new DefaultHttpContext();
    httpContext.TraceIdentifier = "trace-pt";

    var problem = CreateFactory().Create(httpContext, ApiProblemCode.Forbidden, StatusCodes.Status403Forbidden);

    Assert.Equal("Acesso negado", problem.Title);
    Assert.Equal(ApiProblemCode.Forbidden, problem.Extensions[ApiConventions.ErrorCodeExtension]);
    Assert.Equal("trace-pt", problem.Extensions[ApiConventions.TraceIdExtension]);
  }

  [Fact]
  public void FactoryUsesEnglishWhenAcceptLanguageRequestsIt()
  {
    var httpContext = new DefaultHttpContext();
    httpContext.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";

    var problem = CreateFactory().CreateValidation(
      httpContext,
      new Dictionary<string, string[]> { ["name"] = [ValidationMessageKeys.Required] });

    Assert.Equal("Invalid request", problem.Title);
    Assert.Equal(ApiProblemCode.Validation, problem.Extensions[ApiConventions.ErrorCodeExtension]);
    Assert.Equal("Required field.", problem.Errors["name"][0]);
  }

  [Fact]
  public void FactoryLocalizesValidationMessagesToBrazilianPortugueseByDefault()
  {
    var httpContext = new DefaultHttpContext();

    var problem = CreateFactory().CreateValidation(
      httpContext,
      new Dictionary<string, string[]>
      {
        ["name"] = [ValidationMessageKeys.Required],
        ["propertyId"] = ["validation.property"]
      });

    Assert.Equal("Requisi\u00e7\u00e3o inv\u00e1lida", problem.Title);
    Assert.Equal("Campo obrigatório.", problem.Errors["name"][0]);
    Assert.Equal("Informe um imóvel válido.", problem.Errors["propertyId"][0]);
  }

  [Fact]
  public void FactoryLeavesUnknownValidationMessagesUnchanged()
  {
    var httpContext = new DefaultHttpContext();

    var problem = CreateFactory().CreateValidation(
      httpContext,
      new Dictionary<string, string[]> { ["name"] = ["validation.futureRule"] });

    Assert.Equal("validation.futureRule", problem.Errors["name"][0]);
  }

  private static ApiProblemDetailsFactory CreateFactory() =>
    new(new ProblemDetailsMessageCatalog(), new LocalizedValidationMessages());
}
