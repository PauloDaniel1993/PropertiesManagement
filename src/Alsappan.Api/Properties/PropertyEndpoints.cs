using Alsappan.Api.OperationResults;
using Alsappan.Api.Modules;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Properties;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Properties;

#pragma warning disable CA1812
internal sealed class PropertyEndpointModule : IApiEndpointModule
{
  public int Order => 300;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapPropertyEndpoints();
}

internal static class PropertyEndpoints
{
  public static RouteGroupBuilder MapPropertyEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var properties = v1.MapGroup("/properties")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Properties");

    properties.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? status,
          string? type,
          bool? hasGarage,
          decimal? minRent,
          decimal? maxRent,
          string? sort,
          bool? includeArchived,
          string? locale,
          IPropertyService propertyService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new PropertyListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            status,
            type,
            hasGarage,
            minRent,
            maxRent,
            sort,
            includeArchived ?? false,
            locale);
          var result = await propertyService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Properties_List")
      .WithSummary("Lists properties in the active organization.")
      .Produces<PagedResultDto<PropertyListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    properties.MapGet(
        "/status-options",
        async (
          string? locale,
          IPropertyService propertyService,
          CancellationToken cancellationToken) =>
        {
          var options = await propertyService.GetStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Properties_GetStatusOptions")
      .WithSummary("Lists property status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    properties.MapGet(
        "/type-options",
        async (
          string? locale,
          IPropertyService propertyService,
          CancellationToken cancellationToken) =>
        {
          var options = await propertyService.GetTypeOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Properties_GetTypeOptions")
      .WithSummary("Lists property type options.")
      .Produces<IReadOnlyList<SelectOptionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    properties.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IPropertyService propertyService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await propertyService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Properties_Get")
      .WithSummary("Gets property details.")
      .Produces<PropertyDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    properties.MapPost(
        "",
        async (
          PropertyCreateRequestDto request,
          string? locale,
          IPropertyService propertyService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await propertyService.CreateAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Properties_Create")
      .WithSummary("Creates a property in the active organization.")
      .Produces<PropertyDetailDto>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    properties.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          PropertyUpdateRequestDto request,
          string? locale,
          IPropertyService propertyService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await propertyService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Properties_Update")
      .WithSummary("Updates property details.")
      .Produces<PropertyDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    properties.MapPost(
        "/{id:guid}/status",
        async (
          Guid id,
          PropertyStatusChangeRequestDto request,
          string? locale,
          IPropertyService propertyService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await propertyService.ChangeStatusAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Properties_ChangeStatus")
      .WithSummary("Changes property lifecycle status.")
      .Produces<PropertyDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    properties.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IPropertyService propertyService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await propertyService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Properties_Restore")
      .WithSummary("Restores an archived property.")
      .Produces<PropertyDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    properties.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IPropertyService propertyService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await propertyService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Properties_Archive")
      .WithSummary("Archives a property in the active organization.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
