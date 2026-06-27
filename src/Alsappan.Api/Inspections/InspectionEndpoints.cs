using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Inspections;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Inspections;

#pragma warning disable CA1812
internal sealed class InspectionEndpointModule : IApiEndpointModule
{
  public int Order => 390;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapInspectionEndpoints();
}

internal static class InspectionEndpoints
{
  public static RouteGroupBuilder MapInspectionEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var inspections = v1.MapGroup("/inspections")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Inspections");

    inspections.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? type,
          string? status,
          Guid? propertyId,
          Guid? contractId,
          Guid? residentId,
          Guid? assignedUserId,
          DateTimeOffset? scheduledFrom,
          DateTimeOffset? scheduledTo,
          bool? pendingOnly,
          bool? includeArchived,
          string? sort,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new InspectionListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            type,
            status,
            propertyId,
            contractId,
            residentId,
            assignedUserId,
            scheduledFrom,
            scheduledTo,
            pendingOnly ?? false,
            includeArchived ?? false,
            sort,
            locale);
          var result = await inspectionService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_List")
      .WithSummary("Lists inspections in the active organization.")
      .Produces<PagedResultDto<InspectionListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    inspections.MapGet(
        "/type-options",
        async (string? locale, IInspectionService inspectionService, CancellationToken cancellationToken) =>
        {
          var options = await inspectionService.GetTypeOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Inspections_GetTypeOptions")
      .WithSummary("Lists inspection type options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    inspections.MapGet(
        "/status-options",
        async (string? locale, IInspectionService inspectionService, CancellationToken cancellationToken) =>
        {
          var options = await inspectionService.GetStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Inspections_GetStatusOptions")
      .WithSummary("Lists inspection status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    inspections.MapGet(
        "/condition-rating-options",
        async (string? locale, IInspectionService inspectionService, CancellationToken cancellationToken) =>
        {
          var options = await inspectionService.GetConditionRatingOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Inspections_GetConditionRatingOptions")
      .WithSummary("Lists inspection condition rating options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    inspections.MapGet(
        "/document-kind-options",
        async (string? locale, IInspectionService inspectionService, CancellationToken cancellationToken) =>
        {
          var options = await inspectionService.GetDocumentKindOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Inspections_GetDocumentKindOptions")
      .WithSummary("Lists inspection document kind options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    inspections.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_Get")
      .WithSummary("Gets inspection details.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    inspections.MapPost(
        "",
        async (
          InspectionScheduleRequestDto request,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.ScheduleAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_Schedule")
      .WithSummary("Schedules an inspection.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    inspections.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          InspectionUpdateRequestDto request,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_Update")
      .WithSummary("Updates inspection schedule metadata.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    inspections.MapPost(
        "/{id:guid}/start",
        async (
          Guid id,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.StartAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_Start")
      .WithSummary("Starts an inspection.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    inspections.MapPost(
        "/{id:guid}/complete",
        async (
          Guid id,
          InspectionLifecycleRequestDto request,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.CompleteAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_Complete")
      .WithSummary("Completes and locks an inspection.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    inspections.MapPost(
        "/{id:guid}/cancel",
        async (
          Guid id,
          InspectionLifecycleRequestDto request,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.CancelAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_Cancel")
      .WithSummary("Cancels an inspection.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    inspections.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_Restore")
      .WithSummary("Restores an archived inspection.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    inspections.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_Archive")
      .WithSummary("Archives an inspection.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    inspections.MapPost(
        "/{id:guid}/checklist-items",
        async (
          Guid id,
          InspectionChecklistItemRequestDto request,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.AddChecklistItemAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_AddChecklistItem")
      .WithSummary("Adds an inspection checklist item.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    inspections.MapPut(
        "/{id:guid}/checklist-items/{checklistItemId:guid}",
        async (
          Guid id,
          Guid checklistItemId,
          InspectionChecklistItemRequestDto request,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.UpdateChecklistItemAsync(
              id,
              checklistItemId,
              request,
              locale,
              cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_UpdateChecklistItem")
      .WithSummary("Updates an inspection checklist item.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    inspections.MapDelete(
        "/{id:guid}/checklist-items/{checklistItemId:guid}",
        async (
          Guid id,
          Guid checklistItemId,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.DeleteChecklistItemAsync(
              id,
              checklistItemId,
              locale,
              cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_DeleteChecklistItem")
      .WithSummary("Deletes an inspection checklist item.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    inspections.MapPost(
        "/{id:guid}/document-links",
        async (
          Guid id,
          InspectionDocumentLinkRequestDto request,
          string? locale,
          IInspectionService inspectionService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await inspectionService.LinkDocumentAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Inspections_LinkDocument")
      .WithSummary("Links a document or photo to an inspection.")
      .Produces<InspectionDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    return v1;
  }
}
