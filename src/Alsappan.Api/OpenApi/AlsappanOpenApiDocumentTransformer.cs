using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Alsappan.Api.OpenApi;

internal sealed class AlsappanOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
  public Task TransformAsync(
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(document);

    document.Info.Title = "Alsappan API";
    document.Info.Summary = "Property management API contract.";
    document.Info.Description =
      "Versioned REST API for the Alsappan multi-tenant property management platform.";
    document.Info.Version = Api.Contracts.ApiConventions.CurrentVersion;

    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
      Type = SecuritySchemeType.Http,
      Scheme = "bearer",
      BearerFormat = "JWT",
      Description = "JWT access token issued by the Alsappan authentication endpoints."
    };

    return Task.CompletedTask;
  }
}
