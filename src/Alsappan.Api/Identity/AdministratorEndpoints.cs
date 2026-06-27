using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Api.Modules;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Identity;

#pragma warning disable CA1812
internal sealed class AdministratorEndpointModule : IApiEndpointModule
{
  public int Order => 200;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapAdministratorEndpoints();
}

internal static class AdministratorEndpoints
{
  public static RouteGroupBuilder MapAdministratorEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var administrators = v1.MapGroup("/administrators")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Administrators");

    administrators.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? status,
          string? role,
          bool? includeArchived,
          IAdministratorService administratorService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new AdministratorListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            status,
            role,
            includeArchived ?? false);
          var result = await administratorService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Administrators_List")
      .WithSummary("Lists administrators in the active organization.")
      .Produces<PagedResultDto<AdministratorListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    administrators.MapGet(
        "/role-options",
        async (
          IAdministratorService administratorService,
          CancellationToken cancellationToken) =>
        {
          var roles = await administratorService.GetRoleOptionsAsync(cancellationToken)
            .ConfigureAwait(false);

          return Results.Ok(roles);
        })
      .WithName("Administrators_GetRoleOptions")
      .WithSummary("Lists administrator role options.")
      .Produces<IReadOnlyList<SelectOptionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    administrators.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          IAdministratorService administratorService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await administratorService.GetAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Administrators_Get")
      .WithSummary("Gets administrator details.")
      .Produces<AdministratorDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    administrators.MapPost(
        "",
        async (
          AdministratorCreateRequestDto request,
          IAdministratorService administratorService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await administratorService.CreateAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Administrators_Create")
      .WithSummary("Creates or invites an administrator.")
      .Produces<AdministratorDetailDto>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    administrators.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          AdministratorUpdateRequestDto request,
          IAdministratorService administratorService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await administratorService.UpdateAsync(id, request, cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Administrators_Update")
      .WithSummary("Updates administrator profile and roles.")
      .Produces<AdministratorDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    administrators.MapPost(
        "/{id:guid}/deactivate",
        async (
          Guid id,
          IAdministratorService administratorService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await administratorService.DeactivateAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Administrators_Deactivate")
      .WithSummary("Deactivates an administrator and invalidates active sessions.")
      .Produces<AdministratorDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    administrators.MapPost(
        "/{id:guid}/reactivate",
        async (
          Guid id,
          IAdministratorService administratorService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await administratorService.ReactivateAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Administrators_Reactivate")
      .WithSummary("Reactivates an administrator.")
      .Produces<AdministratorDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    administrators.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IAdministratorService administratorService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await administratorService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Administrators_Archive")
      .WithSummary("Archives an administrator in the active organization.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
