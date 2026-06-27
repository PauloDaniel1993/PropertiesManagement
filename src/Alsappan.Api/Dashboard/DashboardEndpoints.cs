using Alsappan.Api.Authorization;
using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Dashboard;

#pragma warning disable CA1812
internal sealed class DashboardEndpointModule : IApiEndpointModule
{
  public int Order => 410;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapDashboardEndpoints();
}

internal static class DashboardEndpoints
{
  public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var dashboard = v1.MapGroup("/dashboard")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Dashboard");

    dashboard.MapGet(
        "",
        async (
          string? locale,
          IDashboardService dashboardService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await dashboardService.GetOverviewAsync(
              new DashboardOverviewRequestDto(locale),
              cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .WithName("Dashboard_GetOverview")
      .RequirePermission(PermissionCodes.Read(PermissionModules.Dashboard))
      .WithSummary("Gets permission-aware dashboard metrics for the active organization.")
      .Produces<DashboardOverviewDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    return v1;
  }
}
