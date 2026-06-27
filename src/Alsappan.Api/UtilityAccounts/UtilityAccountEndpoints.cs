using Alsappan.Api.Authorization;
using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.UtilityAccounts;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.UtilityAccounts;

#pragma warning disable CA1812
internal sealed class UtilityAccountEndpointModule : IApiEndpointModule
{
  public int Order => 370;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapUtilityAccountEndpoints();
}

internal static class UtilityAccountEndpoints
{
  public static RouteGroupBuilder MapUtilityAccountEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var utilityAccounts = v1.MapGroup("/utility-accounts")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Utility accounts");

    utilityAccounts.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? type,
          string? status,
          string? responsibility,
          Guid? propertyId,
          Guid? contractId,
          DateOnly? billingFrom,
          DateOnly? billingTo,
          DateOnly? dueFrom,
          DateOnly? dueTo,
          bool? overdueOnly,
          string? sort,
          bool? includeArchived,
          string? locale,
          IUtilityAccountService utilityAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new UtilityAccountListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            type,
            status,
            responsibility,
            propertyId,
            contractId,
            billingFrom,
            billingTo,
            dueFrom,
            dueTo,
            overdueOnly ?? false,
            sort,
            includeArchived ?? false,
            locale);
          var result = await utilityAccountService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("UtilityAccounts_List")
      .RequirePermission(PermissionCodes.Read(PermissionModules.UtilityAccounts))
      .WithSummary("Lists utility accounts in the active organization.")
      .Produces<PagedResultDto<UtilityAccountListItemDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    utilityAccounts.MapGet(
        "/status-options",
        async (string? locale, IUtilityAccountService utilityAccountService, CancellationToken cancellationToken) =>
        {
          var options = await utilityAccountService.GetStatusOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("UtilityAccounts_GetStatusOptions")
      .WithSummary("Lists utility account status options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    utilityAccounts.MapGet(
        "/type-options",
        async (string? locale, IUtilityAccountService utilityAccountService, CancellationToken cancellationToken) =>
        {
          var options = await utilityAccountService.GetTypeOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("UtilityAccounts_GetTypeOptions")
      .WithSummary("Lists utility account type options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    utilityAccounts.MapGet(
        "/responsibility-options",
        async (string? locale, IUtilityAccountService utilityAccountService, CancellationToken cancellationToken) =>
        {
          var options = await utilityAccountService.GetResponsibilityOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .WithName("UtilityAccounts_GetResponsibilityOptions")
      .WithSummary("Lists utility responsibility options.")
      .Produces<IReadOnlyList<StatusLabelDto>>();

    utilityAccounts.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IUtilityAccountService utilityAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await utilityAccountService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("UtilityAccounts_Get")
      .WithSummary("Gets utility account details.")
      .Produces<UtilityAccountDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    utilityAccounts.MapPost(
        "",
        async (
          UtilityAccountCreateRequestDto request,
          string? locale,
          IUtilityAccountService utilityAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await utilityAccountService.CreateAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("UtilityAccounts_Create")
      .RequirePermission(PermissionCodes.Write(PermissionModules.UtilityAccounts))
      .WithSummary("Creates a utility account.")
      .Produces<UtilityAccountDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    utilityAccounts.MapPut(
        "/{id:guid}",
        async (
          Guid id,
          UtilityAccountUpdateRequestDto request,
          string? locale,
          IUtilityAccountService utilityAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await utilityAccountService.UpdateAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("UtilityAccounts_Update")
      .WithSummary("Updates utility account metadata.")
      .Produces<UtilityAccountDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    utilityAccounts.MapPost(
        "/{id:guid}/mark-paid",
        async (
          Guid id,
          UtilityMarkPaidRequestDto request,
          string? locale,
          IUtilityAccountService utilityAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await utilityAccountService.MarkPaidAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("UtilityAccounts_MarkPaid")
      .RequirePermission(PermissionCodes.Manage(PermissionModules.UtilityAccounts))
      .WithSummary("Marks a utility account as paid.")
      .Produces<UtilityAccountDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    utilityAccounts.MapPost(
        "/{id:guid}/cancel",
        async (
          Guid id,
          UtilityLifecycleRequestDto request,
          string? locale,
          IUtilityAccountService utilityAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await utilityAccountService.CancelAsync(id, request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("UtilityAccounts_Cancel")
      .WithSummary("Cancels a utility account.")
      .Produces<UtilityAccountDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    utilityAccounts.MapPost(
        "/{id:guid}/restore",
        async (
          Guid id,
          string? locale,
          IUtilityAccountService utilityAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await utilityAccountService.RestoreAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("UtilityAccounts_Restore")
      .WithSummary("Restores an archived utility account.")
      .Produces<UtilityAccountDetailDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    utilityAccounts.MapDelete(
        "/{id:guid}",
        async (
          Guid id,
          IUtilityAccountService utilityAccountService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await utilityAccountService.ArchiveAsync(id, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("UtilityAccounts_Archive")
      .WithSummary("Archives a utility account.")
      .Produces(StatusCodes.Status204NoContent)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
