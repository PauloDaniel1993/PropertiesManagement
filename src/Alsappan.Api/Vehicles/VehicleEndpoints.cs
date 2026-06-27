using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Vehicles;

#pragma warning disable CA1812
internal sealed class VehicleEndpointModule : IApiEndpointModule
{
  public int Order => 380;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapVehicleEndpoints();
}

internal static class VehicleEndpoints
{
  public static RouteGroupBuilder MapVehicleEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var vehicles = v1.MapGroup("/vehicles")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Vehicles");

    vehicles.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? plate,
          Guid? residentId,
          Guid? propertyId,
          Guid? contractId,
          string? type,
          string? authorizationStatus,
          bool? hasParkingAllocation,
          string? parkingSpaceIdentifier,
          bool? includeArchived,
          string? sort,
          string? locale,
          IVehicleService vehicleService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new VehicleListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            plate,
            residentId,
            propertyId,
            contractId,
            type,
            authorizationStatus,
            hasParkingAllocation,
            parkingSpaceIdentifier,
            includeArchived ?? false,
            sort,
            locale);
          var result = await vehicleService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Vehicles_List")
      .WithSummary("Lists vehicles in the active organization.")
      .Produces<PagedResultDto<VehicleListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    vehicles.MapGet(
        "/type-options",
        async (string? locale, IVehicleService vehicleService, CancellationToken cancellationToken) =>
        {
          var options = await vehicleService.GetTypeOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Vehicles_GetTypeOptions")
      .WithSummary("Lists vehicle type options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    vehicles.MapGet(
        "/authorization-status-options",
        async (string? locale, IVehicleService vehicleService, CancellationToken cancellationToken) =>
        {
          var options = await vehicleService.GetAuthorizationStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Vehicles_GetAuthorizationStatusOptions")
      .WithSummary("Lists vehicle authorization status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    vehicles.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IVehicleService vehicleService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await vehicleService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Vehicles_Get")
      .WithSummary("Gets vehicle details.")
      .Produces<VehicleDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    vehicles.MapPost(
        "",
        async (
          VehicleCreateRequestDto request,
          string? locale,
          IVehicleService vehicleService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await vehicleService.CreateAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Vehicles_Create")
      .WithSummary("Creates a vehicle.")
      .Produces<VehicleDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    vehicles.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          VehicleUpdateRequestDto request,
          string? locale,
          IVehicleService vehicleService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await vehicleService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Vehicles_Update")
      .WithSummary("Updates vehicle metadata.")
      .Produces<VehicleDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    vehicles.MapPost(
        "/{id:guid}/authorize",
        async (
          Guid id,
          string? locale,
          IVehicleService vehicleService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await vehicleService.AuthorizeAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Vehicles_Authorize")
      .WithSummary("Authorizes a vehicle.")
      .Produces<VehicleDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    vehicles.MapPost(
        "/{id:guid}/deny",
        async (
          Guid id,
          VehicleLifecycleRequestDto request,
          string? locale,
          IVehicleService vehicleService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await vehicleService.DenyAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Vehicles_Deny")
      .WithSummary("Denies a vehicle authorization.")
      .Produces<VehicleDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    vehicles.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IVehicleService vehicleService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await vehicleService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Vehicles_Restore")
      .WithSummary("Restores an archived vehicle.")
      .Produces<VehicleDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    vehicles.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IVehicleService vehicleService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await vehicleService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Vehicles_Archive")
      .WithSummary("Archives a vehicle.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
