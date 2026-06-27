using Alsappan.Api.Authorization;
using Alsappan.Api.Modules;
using Alsappan.Api.OperationResults;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Settings;

#pragma warning disable CA1812
internal sealed class SettingsEndpointModule : IApiEndpointModule
{
  public int Order => 330;

  public void MapEndpoints(RouteGroupBuilder v1) => v1.MapSettingsEndpoints();
}

internal static class SettingsEndpoints
{
  public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder v1)
  {
    ArgumentNullException.ThrowIfNull(v1);

    var settings = v1.MapGroup("/settings")
      .RequireAuthorization("AuthenticatedUser")
      .WithTags("Settings");

    settings.MapGet(
        "",
        async (
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.GetAsync(locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Read(PermissionModules.Settings))
      .WithName("Settings_GetDashboard")
      .WithSummary("Gets organization settings for the active organization.")
      .Produces<SettingsDashboardDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

    settings.MapPut(
        "/organization",
        async (
          OrganizationProfileUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateOrganizationProfileAsync(
              request,
              locale,
              cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Write(PermissionModules.Settings))
      .WithName("Settings_UpdateOrganizationProfile")
      .WithSummary("Updates organization profile settings.")
      .Produces<OrganizationProfileSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapPut(
        "/tenant-behavior",
        async (
          TenantBehaviorUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateTenantBehaviorAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Manage(PermissionModules.Settings))
      .WithName("Settings_UpdateTenantBehavior")
      .WithSummary("Updates tenant behavior settings.")
      .Produces<TenantBehaviorSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapPut(
        "/resident-portal",
        async (
          ResidentPortalUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateResidentPortalAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Write(PermissionModules.Settings))
      .WithName("Settings_UpdateResidentPortal")
      .WithSummary("Updates resident portal settings.")
      .Produces<ResidentPortalSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapPut(
        "/localization",
        async (
          LocalizationUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateLocalizationAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Write(PermissionModules.Settings))
      .WithName("Settings_UpdateLocalization")
      .WithSummary("Updates locale enablement and fallback settings.")
      .Produces<LocalizationSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapGet(
        "/profile/locale",
        async (
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.GetUserLocalePreferenceAsync(locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Read(PermissionModules.Settings))
      .WithName("Settings_GetUserLocalePreference")
      .WithSummary("Gets the current user's locale preference.")
      .Produces<UserLocalePreferenceDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    settings.MapPut(
        "/profile/locale",
        async (
          UserLocalePreferenceUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateUserLocalePreferenceAsync(
              request,
              locale,
              cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Read(PermissionModules.Settings))
      .WithName("Settings_UpdateUserLocalePreference")
      .WithSummary("Updates the current user's locale preference.")
      .Produces<UserLocalePreferenceDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    settings.MapGet(
        "/catalogs",
        async (
          string? catalogType,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.ListCatalogsAsync(catalogType, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Read(PermissionModules.Settings))
      .WithName("Settings_ListCatalogs")
      .WithSummary("Lists configurable domain catalogs.")
      .Produces<IReadOnlyList<DomainCatalogSettingsDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    settings.MapGet(
        "/catalogs/{catalogType}",
        async (
          string catalogType,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.ListCatalogsAsync(catalogType, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Read(PermissionModules.Settings))
      .WithName("Settings_GetCatalog")
      .WithSummary("Gets one configurable domain catalog.")
      .Produces<IReadOnlyList<DomainCatalogSettingsDto>>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    settings.MapPut(
        "/catalogs/{catalogType}",
        async (
          string catalogType,
          DomainCatalogUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateCatalogAsync(
              catalogType,
              request,
              locale,
              cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Manage(PermissionModules.Settings))
      .WithName("Settings_UpdateCatalog")
      .WithSummary("Updates a configurable domain catalog.")
      .Produces<DomainCatalogSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapPut(
        "/security",
        async (
          SecuritySettingsUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateSecurityAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Manage(PermissionModules.Settings))
      .WithName("Settings_UpdateSecurity")
      .WithSummary("Updates session, password, and MFA policy settings.")
      .Produces<SecuritySettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapPut(
        "/notifications",
        async (
          NotificationSettingsUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateNotificationsAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Manage(PermissionModules.Settings))
      .WithName("Settings_UpdateNotifications")
      .WithSummary("Updates notification category and channel settings.")
      .Produces<NotificationSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapPut(
        "/branding",
        async (
          OrganizationBrandingUpdateRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UpdateBrandingAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Write(PermissionModules.Settings))
      .WithName("Settings_UpdateBranding")
      .WithSummary("Updates organization branding tokens and support metadata.")
      .Produces<OrganizationBrandingSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapPost(
        "/branding/logo",
        async (
          BrandLogoUploadRequestDto request,
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.UploadLogoAsync(request, locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Manage(PermissionModules.Settings))
      .WithName("Settings_UploadLogo")
      .WithSummary("Uploads an organization logo through the configured file storage provider.")
      .Produces<OrganizationBrandingSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
      .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

    settings.MapDelete(
        "/branding/logo",
        async (
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.RemoveLogoAsync(locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Manage(PermissionModules.Settings))
      .WithName("Settings_RemoveLogo")
      .WithSummary("Removes the active organization logo.")
      .Produces<OrganizationBrandingSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    settings.MapPost(
        "/branding/reset",
        async (
          string? locale,
          ISettingsService settingsService,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          var result = await settingsService.ResetBrandingAsync(locale, cancellationToken)
            .ConfigureAwait(false);

          return ApplicationEndpointResults.FromOperationResult(httpContext, result);
        })
      .RequirePermission(PermissionCodes.Manage(PermissionModules.Settings))
      .WithName("Settings_ResetBranding")
      .WithSummary("Resets organization branding to the default Alsappan design.")
      .Produces<OrganizationBrandingSettingsDto>()
      .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
      .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    return v1;
  }
}
