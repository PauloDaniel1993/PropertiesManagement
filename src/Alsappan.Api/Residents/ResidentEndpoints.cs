using Alsappan.Api.OperationResults;
using Alsappan.Api.Modules;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Residents;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Residents;

#pragma warning disable CA1812
internal sealed class ResidentEndpointModule : IApiEndpointModule
{
  public int Order => 310;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapResidentEndpoints();
}

internal static class ResidentEndpoints
{
  public static RouteGroupBuilder MapResidentEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var residents = v1.MapGroup("/residents")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Residents");

    residents.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? status,
          string? portalStatus,
          bool? hasPortalAccess,
          string? sort,
          bool? includeArchived,
          string? locale,
          IResidentService residentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new ResidentListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            status,
            portalStatus,
            hasPortalAccess,
            sort,
            includeArchived ?? false,
            locale);
          var result = await residentService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_List")
      .WithSummary("Lists residents in the active organization.")
      .Produces<PagedResultDto<ResidentListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    residents.MapGet(
        "/status-options",
        async (
          string? locale,
          IResidentService residentService,
          CancellationToken cancellationToken) =>
        {
          var options = await residentService.GetStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Residents_GetStatusOptions")
      .WithSummary("Lists resident status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    residents.MapGet(
        "/portal-status-options",
        async (
          string? locale,
          IResidentService residentService,
          CancellationToken cancellationToken) =>
        {
          var options = await residentService.GetPortalStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Residents_GetPortalStatusOptions")
      .WithSummary("Lists resident portal status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    residents.MapGet(
        "/privacy-flag-options",
        async (
          string? locale,
          IResidentService residentService,
          CancellationToken cancellationToken) =>
        {
          var options = await residentService.GetPrivacyFlagOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Residents_GetPrivacyFlagOptions")
      .WithSummary("Lists resident privacy flag options.")
      .Produces<IReadOnlyList<SelectOptionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    residents.MapGet(
        "/duplicate-warnings",
        async (
          string? email,
          string? phone,
          string? documentIdentifier,
          Guid? ignoreResidentId,
          IResidentService residentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new ResidentDuplicateWarningRequestDto(
            email,
            phone,
            documentIdentifier,
            ignoreResidentId);
          var result = await residentService.GetDuplicateWarningsAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_GetDuplicateWarnings")
      .WithSummary("Finds likely duplicate residents before save.")
      .Produces<IReadOnlyList<ResidentDuplicateWarningDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    residents.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IResidentService residentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_Get")
      .WithSummary("Gets resident details.")
      .Produces<ResidentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    residents.MapPost(
        "/{id:guid}/portal-account/invite",
        async (
          Guid id,
          ResidentAccountInviteRequestDto request,
          string? locale,
          IResidentAccountService residentAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentAccountService.InviteAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_InvitePortalAccount")
      .WithSummary("Invites a resident to activate portal access.")
      .Produces<ResidentAccountAccessDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    residents.MapPost(
        "/{id:guid}/portal-account/activate",
        async (
          Guid id,
          ResidentAccountActivateRequestDto request,
          string? locale,
          IResidentAccountService residentAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentAccountService.ActivateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_ActivatePortalAccount")
      .WithSummary("Activates a linked resident portal account.")
      .Produces<ResidentAccountAccessDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    residents.MapPost(
        "/{id:guid}/portal-account/deactivate",
        async (
          Guid id,
          ResidentAccountLifecycleRequestDto request,
          string? locale,
          IResidentAccountService residentAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentAccountService.DeactivateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_DeactivatePortalAccount")
      .WithSummary("Deactivates a resident portal account and revokes active sessions.")
      .Produces<ResidentAccountAccessDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    residents.MapPost(
        "/{id:guid}/portal-account/password-reset",
        async (
          Guid id,
          ResidentAccountPasswordResetRequestDto request,
          string? locale,
          IResidentAccountService residentAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentAccountService.ResetPasswordAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_ResetPortalAccountPassword")
      .WithSummary("Resets a resident portal account password.")
      .Produces<ResidentAccountAccessDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    residents.MapPost(
        "/{id:guid}/portal-account/link",
        async (
          Guid id,
          ResidentAccountLinkRequestDto request,
          string? locale,
          IResidentAccountService residentAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentAccountService.LinkAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_LinkPortalAccount")
      .WithSummary("Links an existing resident user account to a resident record.")
      .Produces<ResidentAccountAccessDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    residents.MapPost(
        "/{id:guid}/portal-account/unlink",
        async (
          Guid id,
          ResidentAccountLifecycleRequestDto request,
          string? locale,
          IResidentAccountService residentAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentAccountService.UnlinkAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_UnlinkPortalAccount")
      .WithSummary("Unlinks resident portal access from a resident record.")
      .Produces<ResidentAccountAccessDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    residents.MapPost(
        "",
        async (
          ResidentCreateRequestDto request,
          string? locale,
          IResidentService residentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentService.CreateAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_Create")
      .WithSummary("Creates a resident in the active organization.")
      .Produces<ResidentDetailDto>(StatusCodes.Status200OK)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    residents.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          ResidentUpdateRequestDto request,
          string? locale,
          IResidentService residentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_Update")
      .WithSummary("Updates resident details.")
      .Produces<ResidentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    residents.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IResidentService residentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_Restore")
      .WithSummary("Restores an archived resident.")
      .Produces<ResidentDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    residents.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IResidentService residentService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await residentService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Residents_Archive")
      .WithSummary("Archives a resident in the active organization.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
