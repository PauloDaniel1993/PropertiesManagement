using Alsappan.Api.Authorization;
using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Audit;

#pragma warning disable CA1812
internal sealed class AuditEndpointModule : IApiEndpointModule
{
  public int Order => 820;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapAuditEndpoints();
}

internal static class AuditEndpoints
{
  public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var audit = v1.MapGroup("/audit")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Audit");

    audit.MapGet(
        "",
        async (
          int? page,
          int? pageSize,
          string? search,
          string? action,
          string? actor,
          string? entityType,
          string? entityId,
          string? category,
          DateOnly? from,
          DateOnly? to,
          string? sort,
          string? locale,
          IAuditService auditService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var request = new AuditListRequestDto(
            page ?? ListFilterDto.DefaultPage,
            pageSize ?? ListFilterDto.DefaultPageSize,
            search,
            action,
            actor,
            entityType,
            entityId,
            category,
            from,
            to,
            sort,
            locale);
          var result = await auditService.ListAsync(request, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Read(PermissionModules.Audit))
      .WithName("Audit_List")
      .WithSummary("Lists immutable audit entries for the active organization.")
      .Produces<PagedResultDto<AuditEntryDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    audit.MapGet(
        "/category-options",
        async (
          string? locale,
          IAuditService auditService,
          CancellationToken cancellationToken) =>
        {
          var options = await auditService.GetCategoryOptionsAsync(locale, cancellationToken)
            .ConfigureAwait(false);
          return Results.Ok(options);
        })
      .RequirePermission(PermissionCodes.Read(PermissionModules.Audit))
      .WithName("Audit_GetCategoryOptions")
      .WithSummary("Lists audit category options.")
      .Produces<IReadOnlyList<AuditCategoryLabelDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    audit.MapGet(
        "/{id:guid}",
        async (
          Guid id,
          string? locale,
          IAuditService auditService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await auditService.GetAsync(id, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Read(PermissionModules.Audit))
      .WithName("Audit_Get")
      .WithSummary("Gets an immutable audit entry.")
      .Produces<AuditEntryDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    return v1;
  }
}
