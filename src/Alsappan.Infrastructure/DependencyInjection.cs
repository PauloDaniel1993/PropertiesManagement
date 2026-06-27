using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Seeding;
using Alsappan.Infrastructure.Modules;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Alsappan.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services) =>
    AddInfrastructureCore(services, environmentName: null, configuration: null);

  public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    string environmentName,
    IConfiguration configuration)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
    ArgumentNullException.ThrowIfNull(configuration);

    return AddInfrastructureCore(services, environmentName, configuration);
  }

  private static IServiceCollection AddInfrastructureCore(
    IServiceCollection services,
    string? environmentName,
    IConfiguration? configuration)
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

    services.AddInfrastructureModules();

    if (DemoSeedContributor.IsEnabled(configuration, environmentName))
    {
      services.AddSingleton(new DemoSeedEnvironment(environmentName!));
      services.AddScoped<IDatabaseSeedContributor, DemoSeedContributor>();
    }

    return services;
  }
}
