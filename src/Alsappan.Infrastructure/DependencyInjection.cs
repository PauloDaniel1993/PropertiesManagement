using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Files;
using Alsappan.Application.Common.Seeding;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Identity.Administrators;
using Alsappan.Application.Identity.Auth;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Identity.Security;
using Alsappan.Application.Identity.Sessions;
using Alsappan.Infrastructure.Audit;
using Alsappan.Infrastructure.Auth;
using Alsappan.Infrastructure.Authorization;
using Alsappan.Infrastructure.Identity;
using Alsappan.Infrastructure.Notifications;
using Alsappan.Infrastructure.Outbox;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Seeding;
using Alsappan.Infrastructure.Storage;
using Alsappan.Infrastructure.Tenancy;
using Alsappan.Infrastructure.Timeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Alsappan.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton(TimeProvider.System);
    services.AddSingleton(serviceProvider =>
      serviceProvider.GetRequiredService<IOptions<AlsappanOptions>>().Value.Database);
    services.AddSingleton(serviceProvider =>
      serviceProvider.GetRequiredService<IOptions<AlsappanOptions>>().Value.Auth);
    services.AddSingleton(serviceProvider =>
      serviceProvider.GetRequiredService<IOptions<AlsappanOptions>>().Value.Storage);

    services.AddDbContext<AlsappanDbContext>((serviceProvider, options) =>
    {
      var databaseOptions = serviceProvider.GetRequiredService<DatabaseOptions>();
      options.UseAlsappanNpgsql(databaseOptions);
    });

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

    services.AddScoped<IAuditWriter, EfAuditWriter>();
    services.AddScoped<IModuleEventOutboxWriter, EfModuleEventOutboxWriter>();
    services.AddScoped<ITimelineProjectionWriter, EfTimelineProjectionWriter>();
    services.AddScoped<INotificationDispatcher, EfNotificationDispatcher>();
    services.AddScoped<IModuleEventDispatcher, ModuleEventDispatcher>();
    services.AddScoped<OutboxProcessor>();

    services.AddScoped<IFileStorageProvider, LocalFileStorageProvider>();
    services.AddScoped<IDatabaseSeedRunner, EfDatabaseSeedRunner>();
    services.AddScoped<IDatabaseSeedContributor, IdentitySeedContributor>();

    return services;
  }
}
