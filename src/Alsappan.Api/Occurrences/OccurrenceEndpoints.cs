using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Occurrences;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Occurrences;

#pragma warning disable CA1812
internal sealed class OccurrenceEndpointModule : IApiEndpointModule
{
  public int Order => 390;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapOccurrenceEndpoints();
}

internal static class OccurrenceEndpoints
{
  public static RouteGroupBuilder MapOccurrenceEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var occurrences = v1.MapGroup("/occurrences")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Occurrences");

    occurrences.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? type,
          string? priority,
          string? status,
          Guid? assignedUserId,
          Guid? propertyId,
          Guid? residentId,
          Guid? contractId,
          DateOnly? dateFrom,
          DateOnly? dateTo,
          bool? unresolvedOnly,
          bool? includeArchived,
          string? sort,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new OccurrenceListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            type,
            priority,
            status,
            assignedUserId,
            propertyId,
            residentId,
            contractId,
            dateFrom,
            dateTo,
            unresolvedOnly ?? false,
            includeArchived ?? false,
            sort,
            locale);
          var result = await occurrenceService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_List")
      .WithSummary("Lists occurrences in the active organization.")
      .Produces<PagedResultDto<OccurrenceListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    occurrences.MapGet(
        "/type-options",
        async (string? locale, IOccurrenceService occurrenceService, CancellationToken cancellationToken) =>
        {
          var options = await occurrenceService.GetTypeOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Occurrences_GetTypeOptions")
      .WithSummary("Lists occurrence type options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    occurrences.MapGet(
        "/priority-options",
        async (string? locale, IOccurrenceService occurrenceService, CancellationToken cancellationToken) =>
        {
          var options = await occurrenceService.GetPriorityOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Occurrences_GetPriorityOptions")
      .WithSummary("Lists occurrence priority options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    occurrences.MapGet(
        "/status-options",
        async (string? locale, IOccurrenceService occurrenceService, CancellationToken cancellationToken) =>
        {
          var options = await occurrenceService.GetStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("Occurrences_GetStatusOptions")
      .WithSummary("Lists occurrence status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    occurrences.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_Get")
      .WithSummary("Gets occurrence details.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    occurrences.MapPost(
        "",
        async (
          OccurrenceCreateRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.CreateAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_Create")
      .WithSummary("Creates an occurrence.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          OccurrenceUpdateRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_Update")
      .WithSummary("Updates occurrence metadata.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPost(
        "/{id:guid}/assign",
        async (
          Guid id,
          OccurrenceAssignmentRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.AssignAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_Assign")
      .WithSummary("Assigns or unassigns an occurrence.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPost(
        "/{id:guid}/priority",
        async (
          Guid id,
          OccurrencePriorityChangeRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.ChangePriorityAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_ChangePriority")
      .WithSummary("Changes occurrence priority.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPost(
        "/{id:guid}/status",
        async (
          Guid id,
          OccurrenceStatusChangeRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.ChangeStatusAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_ChangeStatus")
      .WithSummary("Changes occurrence status.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPost(
        "/{id:guid}/resolve",
        async (
          Guid id,
          OccurrenceResolutionRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.ResolveAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_Resolve")
      .WithSummary("Resolves an occurrence.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPost(
        "/{id:guid}/cancel",
        async (
          Guid id,
          OccurrenceLifecycleRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.CancelAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_Cancel")
      .WithSummary("Cancels an occurrence.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPost(
        "/{id:guid}/comments",
        async (
          Guid id,
          OccurrenceCommentRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.AddCommentAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_AddComment")
      .WithSummary("Adds an occurrence comment.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPost(
        "/{id:guid}/attachments",
        async (
          Guid id,
          OccurrenceAttachmentRequestDto request,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.AttachDocumentAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_AttachDocument")
      .WithSummary("Links a document to an occurrence.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    occurrences.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_Restore")
      .WithSummary("Restores an archived occurrence.")
      .Produces<OccurrenceDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    occurrences.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IOccurrenceService occurrenceService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await occurrenceService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Occurrences_Archive")
      .WithSummary("Archives an occurrence.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
