using Alsappan.Application.Common.Configuration;
using Alsappan.Infrastructure.Modules;
using Alsappan.Infrastructure.Persistence;
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

    services.AddInfrastructureModules();

    return services;
  }
}
