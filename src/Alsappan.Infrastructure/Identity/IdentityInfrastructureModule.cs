using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Application.Identity.Auth;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Identity.Security;
using Alsappan.Application.Identity.Sessions;
using Alsappan.Infrastructure.Auth;
using Alsappan.Infrastructure.Authorization;
using Alsappan.Infrastructure.Modules;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Identity;

#pragma warning disable CA1812
internal sealed class IdentityInfrastructureModule : IInfrastructureModule
{
  public int Order => 100;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IAuthenticatedUserProvider, ClaimsAuthenticatedUserProvider>();
    services.AddScoped<IActiveOrganizationContextResolver, ClaimsActiveOrganizationContextResolver>();
    services.AddScoped<IActiveOrganizationContext, ResolvedActiveOrganizationContext>();
    services.AddSingleton<IRolePermissionCatalog, DefaultRolePermissionCatalog>();
    services.AddScoped<IPermissionService, DefaultPermissionService>();

    services.AddScoped<IAccessTokenService, JwtAccessTokenService>();
    services.AddSingleton<IRefreshTokenProtector, Sha256RefreshTokenProtector>();
    services.AddScoped<IRefreshSessionStore, EfRefreshSessionStore>();

    services.AddScoped<IIdentityRepository, EfIdentityRepository>();
    services.AddScoped<IIdentitySessionInvalidator, EfIdentitySessionInvalidator>();
    services.AddScoped<IPasswordHashService, Pbkdf2PasswordHashService>();
    services.AddScoped<IIdentityAuthService, IdentityAuthService>();
    services.AddScoped<IAdministratorService, AdministratorService>();
  }
}
