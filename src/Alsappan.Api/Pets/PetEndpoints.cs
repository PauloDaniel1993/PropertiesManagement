using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Pets;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Pets;

#pragma warning disable CA1812
internal sealed class PetEndpointModule : IApiEndpointModule
{
  public int Order => 375;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapPetEndpoints();
}

internal static class PetEndpoints
{
  public static RouteGroupBuilder MapPetEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var pets = v1.MapGroup("/pets")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Pets");

    pets.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          Guid? residentId,
          Guid? propertyId,
          Guid? contractId,
          string? species,
          string? authorizationStatus,
          bool? activeContractOnly,
          string? sort,
          bool? includeArchived,
          string? locale,
          IPetService petService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new PetListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            residentId,
            propertyId,
            contractId,
            species,
            authorizationStatus,
            activeContractOnly ?? false,
            sort,
            includeArchived ?? false,
            locale);
          var result = await petService.ListAsync(request, cancellationToken).ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Pets_List")
      .WithSummary("Lists pets in the active organization.")
      .Produces<PagedResultDto<PetListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    pets.MapGet(
        "/options",
        async (string? locale, IPetService petService, CancellationToken cancellationToken) =>
        {
          var options = await petService.GetOptionsAsync(locale, cancellationToken).ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Pets_GetOptions")
      .WithSummary("Lists pet species, authorization status, and document kind options.")
      .Produces<PetOptionsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    pets.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IPetService petService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await petService.GetAsync(id, locale, cancellationToken).ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Pets_Get")
      .WithSummary("Gets pet details.")
      .Produces<PetDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    pets.MapPost(
        "",
        async (
          PetCreateRequestDto request,
          string? locale,
          IPetService petService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await petService.CreateAsync(request, locale, cancellationToken).ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Pets_Create")
      .WithSummary("Creates a pet.")
      .Produces<PetDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    pets.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          PetUpdateRequestDto request,
          string? locale,
          IPetService petService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await petService.UpdateAsync(id, request, locale, cancellationToken).ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Pets_Update")
      .WithSummary("Updates pet metadata.")
      .Produces<PetDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    pets.MapPost(
        "/{id:guid}/authorize",
        async (
          Guid id,
          PetLifecycleRequestDto request,
          string? locale,
          IPetService petService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await petService.AuthorizeAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Pets_Authorize")
      .WithSummary("Authorizes a pet.")
      .Produces<PetDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    pets.MapPost(
        "/{id:guid}/deny",
        async (
          Guid id,
          PetLifecycleRequestDto request,
          string? locale,
          IPetService petService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await petService.DenyAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Pets_Deny")
      .WithSummary("Denies a pet authorization request.")
      .Produces<PetDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    pets.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IPetService petService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await petService.RestoreAsync(id, locale, cancellationToken).ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Pets_Restore")
      .WithSummary("Restores an archived pet.")
      .Produces<PetDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    pets.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IPetService petService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await petService.ArchiveAsync(id, cancellationToken).ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Pets_Archive")
      .WithSummary("Archives a pet.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
