using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Notifications;

#pragma warning disable CA1812
internal sealed class NotificationEndpointModule : IApiEndpointModule
{
  public int Order => 320;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapNotificationEndpoints();
}

internal static class NotificationEndpoints
{
  public static RouteGroupBuilder MapNotificationEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var notifications = v1.MapGroup("/notifications")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Notifications");

    notifications.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? category,
          bool? isRead,
          string? channel,
          bool? includeArchived,
          string? sort,
          string? locale,
          INotificationService notificationService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new NotificationListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            category,
            isRead,
            channel,
            includeArchived ?? false,
            sort,
            locale);
          var result = await notificationService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Notifications_List")
      .WithSummary("Lists notifications for the active organization and current user.")
      .Produces<PagedResultDto<NotificationListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    notifications.MapGet(
        "/unread-count",
        async (
          INotificationService notificationService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await notificationService.GetUnreadCountAsync(cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Notifications_GetUnreadCount")
      .WithSummary("Gets the current user's unread notification count.")
      .Produces<NotificationUnreadCountDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    notifications.MapGet(
        "/category-options",
        async (
          string? locale,
          INotificationService notificationService,
          CancellationToken cancellationToken) =>
        {
          var options = await notificationService.GetCategoryOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);

          return Results.Ok(options);
        })
      .WithName("Notifications_GetCategoryOptions")
      .WithSummary("Lists notification categories.")
      .Produces<IReadOnlyList<SelectOptionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

    notifications.MapGet(
        "/channel-options",
        async (
          string? locale,
          INotificationService notificationService,
          CancellationToken cancellationToken) =>
        {
          var options = await notificationService.GetChannelOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);

          return Results.Ok(options);
        })
      .WithName("Notifications_GetChannelOptions")
      .WithSummary("Lists supported notification channels.")
      .Produces<IReadOnlyList<SelectOptionDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

    notifications.MapGet(
        "/preferences",
        async (
          string? locale,
          INotificationService notificationService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await notificationService.GetPreferencesAsync(locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Notifications_GetPreferences")
      .WithSummary("Gets notification preference defaults for the current user.")
      .Produces<NotificationPreferencesDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    notifications.MapPut(
        "/preferences",
        async (
          NotificationPreferenceUpdateRequestDto request,
          string? locale,
          INotificationService notificationService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await notificationService.UpdatePreferencesAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Notifications_UpdatePreferences")
      .WithSummary("Accepts notification preference updates for the current user.")
      .Produces<NotificationPreferencesDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    notifications.MapPost(
        "/read-all",
        async (
          INotificationService notificationService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await notificationService.MarkAllReadAsync(cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Notifications_MarkAllRead")
      .WithSummary("Marks all visible notifications as read for the current user.")
      .Produces<NotificationMarkAllReadResultDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    notifications.MapPost(
        "/{id:guid}/read",
        async (
          Guid id,
          string? locale,
          INotificationService notificationService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await notificationService.MarkReadAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Notifications_MarkRead")
      .WithSummary("Marks a notification as read.")
      .Produces<NotificationListItemDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    notifications.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          INotificationService notificationService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await notificationService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Notifications_Archive")
      .WithSummary("Archives a notification.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
