using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Search;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Search;

#pragma warning disable CA1812
internal sealed class SearchEndpointModule : IApiEndpointModule
{
  public int Order => 420;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapSearchEndpoints();
}

internal static class SearchEndpoints
{
  public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var search = v1.MapGroup("/search")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Search");

    search.MapGet(
        "",
        async (
          string? query,
          int? limit,
          string? locale,
          IGlobalSearchService searchService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await searchService.SearchAsync(
              new GlobalSearchRequestDto(query ?? string.Empty, limit ?? 5, locale),
              cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Search_Global")
      .WithSummary("Searches readable records across global entity types.")
      .Produces<GlobalSearchResponseDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    search.MapGet(
        "/contract",
        async (string? locale, IGlobalSearchService searchService, CancellationToken cancellationToken) =>
        {
          var contract = await searchService.GetContractAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(contract);
        })
      .WithName("Search_GetContract")
      .WithSummary("Describes global search result entity types and matched field labels.")
      .Produces<GlobalSearchResultContractDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

    return v1;
  }
}
