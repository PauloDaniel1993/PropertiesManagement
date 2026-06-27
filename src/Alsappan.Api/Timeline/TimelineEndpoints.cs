using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Timeline;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Timeline;

#pragma warning disable CA1812
internal sealed class TimelineEndpointModule : IApiEndpointModule
{
  public int Order => 500;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapTimelineEndpoints();
}

internal static class TimelineEndpoints
{
  public static RouteGroupBuilder MapTimelineEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var timeline = v1.MapGroup("/timeline")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Timeline");

    timeline.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? entityType,
          string? eventType,
          Guid? actorUserId,
          Guid? actorId,
          DateOnly? from,
          DateOnly? to,
          string? relatedEntityType,
          string? relatedEntityId,
          string? sort,
          string? locale,
          ITimelineService timelineService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new TimelineListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            entityType,
            eventType,
            actorUserId ?? actorId,
            from,
            to,
            relatedEntityType,
            relatedEntityId,
            sort,
            locale);
          var result = await timelineService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Timeline_List")
      .WithSummary("Lists timeline entries in the active organization.")
      .Produces<PagedResultDto<TimelineEntryDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    timeline.MapGet(
        "/entities/{entityType}/{entityId}",
        async (
          string entityType,
          string entityId,
          int? page,
          int? pageSize,
          string? eventType,
          Guid? actorUserId,
          Guid? actorId,
          DateOnly? from,
          DateOnly? to,
          string? relatedEntityType,
          string? relatedEntityId,
          string? sort,
          string? locale,
          ITimelineService timelineService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new TimelineEntityListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            eventType,
            actorUserId ?? actorId,
            from,
            to,
            relatedEntityType,
            relatedEntityId,
            sort,
            locale);
          var result = await timelineService.ListEntityAsync(
              entityType,
              entityId,
              request,
              cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Timeline_ListEntity")
      .WithSummary("Lists timeline entries related to one entity.")
      .Produces<PagedResultDto<TimelineEntryDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    return v1;
  }
}
