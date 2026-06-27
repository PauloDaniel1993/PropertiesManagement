using Alsappan.Application.Identity.Auth;
using Alsappan.Api.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Alsappan.Api.Identity;

#pragma warning disable CA1812
internal sealed class AuthEndpointModule : IApiEndpointModule
{
  public int Order => 100;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapAuthEndpoints();
}

internal static class AuthEndpoints
{
  public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var auth = v1.MapGroup("/auth")
      .WithTags("Identity");

    auth.MapPost(
        "/admin/login",
        async (
          AdminLoginRequest request,
          IIdentityAuthService authService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await authService.LoginAdminAsync(
              request,
              CreateRequestContext(httpContext),
              cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromServiceResult(httpContext, result);
        })
      .AllowAnonymous()
      .WithName("Identity_AdminLogin")
      .WithSummary("Authenticates an administrator user.")
      .Produces<AuthSessionDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

    auth.MapPost(
        "/resident/login",
        async (
          ResidentLoginRequest request,
          IIdentityAuthService authService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await authService.LoginResidentAsync(
              request,
              CreateRequestContext(httpContext),
              cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromServiceResult(httpContext, result);
        })
      .AllowAnonymous()
      .WithName("Identity_ResidentLogin")
      .WithSummary("Authenticates a resident portal user.")
      .Produces<AuthSessionDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

    auth.MapPost(
        "/refresh",
        async (
          RefreshAuthSessionRequest request,
          IIdentityAuthService authService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await authService.RefreshAsync(
              request,
              CreateRequestContext(httpContext),
              cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromServiceResult(httpContext, result);
        })
      .AllowAnonymous()
      .WithName("Identity_RefreshSession")
      .WithSummary("Rotates a refresh token and returns a new access session.")
      .Produces<AuthSessionDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

    auth.MapPost(
        "/logout",
        async (
          LogoutAuthSessionRequest request,
          IIdentityAuthService authService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await authService.LogoutAsync(
              request,
              CreateRequestContext(httpContext),
              cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromServiceResult(httpContext, result);
        })
      .AllowAnonymous()
      .WithName("Identity_Logout")
      .WithSummary("Revokes the current refresh token when one is supplied.")
      .Produces(StatusCodes.Status204NoContent);

    auth.MapGet(
        "/me",
        async (
          IIdentityAuthService authService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await authService.GetCurrentUserAsync(cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromServiceResult(httpContext, result);
        })
      .RequireAuthorization("AuthenticatedUser")
      .WithName("Identity_GetCurrentUser")
      .WithSummary("Returns the authenticated user and active organization context.")
      .Produces<CurrentUserDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    auth.MapPost(
        "/switch-organization",
        async (
          SwitchOrganizationRequest request,
          IIdentityAuthService authService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await authService.SwitchOrganizationAsync(
              request,
              CreateRequestContext(httpContext),
              cancellationToken)
            .ConfigureAwait(false);

          return IdentityEndpointResults.FromServiceResult(httpContext, result);
        })
      .RequireAuthorization("AuthenticatedUser")
      .WithName("Identity_SwitchOrganization")
      .WithSummary("Switches the authenticated user's active organization.")
      .Produces<AuthSessionDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    return v1;
  }

  private static IdentityRequestContext CreateRequestContext(HttpContext httpContext)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    return new IdentityRequestContext(
      httpContext.Request.Headers[HeaderNames.UserAgent].ToString(),
      httpContext.Connection.RemoteIpAddress?.ToString(),
      httpContext.TraceIdentifier,
      httpContext.Request.Headers.AcceptLanguage.ToString());
  }
}
