using Alsappan.Api.Contracts;
using Alsappan.Api.OpenApi;
using Microsoft.OpenApi;

namespace Alsappan.Api.Tests;

public sealed class OpenApiContractTests
{
  [Fact]
  public async Task DocumentTransformerAddsApiMetadataAndBearerSecurityScheme()
  {
    var document = new OpenApiDocument
    {
      Info = new OpenApiInfo(),
      Components = new OpenApiComponents()
    };
    var transformer = new AlsappanOpenApiDocumentTransformer();

    await transformer.TransformAsync(document, null!, CancellationToken.None);

    var securitySchemes = Assert.IsAssignableFrom<IDictionary<string, IOpenApiSecurityScheme>>(
      document.Components.SecuritySchemes);

    Assert.Equal("Alsappan API", document.Info.Title);
    Assert.Equal(ApiConventions.CurrentVersion, document.Info.Version);
    Assert.True(securitySchemes.ContainsKey("Bearer"));
    Assert.Equal(SecuritySchemeType.Http, securitySchemes["Bearer"].Type);
    Assert.Equal("bearer", securitySchemes["Bearer"].Scheme);
  }
}
