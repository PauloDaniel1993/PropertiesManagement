using Alsappan.Api.Contracts;
using Alsappan.Api.Errors;
using Microsoft.AspNetCore.Http;

namespace Alsappan.Api.Tests;

public sealed class ProblemDetailsContractTests
{
  [Fact]
  public void FactoryUsesBrazilianPortugueseByDefault()
  {
    var httpContext = new DefaultHttpContext();
    httpContext.TraceIdentifier = "trace-pt";

    var factory = new ApiProblemDetailsFactory(new ProblemDetailsMessageCatalog());

    var problem = factory.Create(httpContext, ApiProblemCode.Forbidden, StatusCodes.Status403Forbidden);

    Assert.Equal("Acesso negado", problem.Title);
    Assert.Equal(ApiProblemCode.Forbidden, problem.Extensions[ApiConventions.ErrorCodeExtension]);
    Assert.Equal("trace-pt", problem.Extensions[ApiConventions.TraceIdExtension]);
  }

  [Fact]
  public void FactoryUsesEnglishWhenAcceptLanguageRequestsIt()
  {
    var httpContext = new DefaultHttpContext();
    httpContext.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";

    var factory = new ApiProblemDetailsFactory(new ProblemDetailsMessageCatalog());

    var problem = factory.CreateValidation(
      httpContext,
      new Dictionary<string, string[]> { ["name"] = ["The name field is required."] });

    Assert.Equal("Invalid request", problem.Title);
    Assert.Equal(ApiProblemCode.Validation, problem.Extensions[ApiConventions.ErrorCodeExtension]);
    Assert.True(problem.Errors.ContainsKey("name"));
  }
}
