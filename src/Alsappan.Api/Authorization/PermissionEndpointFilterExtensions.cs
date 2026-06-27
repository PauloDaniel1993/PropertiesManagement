using Alsappan.Api.Errors;
using Alsappan.Application.Common.Authorization;

namespace Alsappan.Api.Authorization;

internal static class PermissionEndpointFilterExtensions
{
  public static RouteHandlerBuilder RequirePermission(
    this RouteHandlerBuilder builder,
    string permissionCode)
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

    return builder.AddEndpointFilter(async (context, next) =>
    {
      var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
      var result = await permissionService.AuthorizeAsync(
          permissionCode,
          context.HttpContext.RequestAborted)
        .ConfigureAwait(false);

      if (result.IsGranted)
      {
        return await next(context).ConfigureAwait(false);
      }

      return result.Failure == PermissionEvaluationFailure.Unauthenticated
        ? ApiProblemResults.Unauthorized(context.HttpContext)
        : ApiProblemResults.Forbidden(context.HttpContext);
    });
  }

  public static RouteHandlerBuilder RequireAnyOrganizationPermission(this RouteHandlerBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    return builder.AddEndpointFilter(async (context, next) =>
    {
      var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
      var permissions = await permissionService.GetEffectivePermissionsAsync(context.HttpContext.RequestAborted)
        .ConfigureAwait(false);

      if (permissions.Count > 0)
      {
        return await next(context).ConfigureAwait(false);
      }

      return ApiProblemResults.Forbidden(context.HttpContext);
    });
  }
}
