using Alsappan.Application.Common.Seeding;
using Alsappan.Infrastructure.Identity;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Seeding;

#pragma warning disable CA1812
internal sealed class SeedingInfrastructureModule : IInfrastructureModule
{
  public int Order => 910;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IDatabaseSeedRunner, EfDatabaseSeedRunner>();
    services.AddScoped<IDatabaseSeedContributor, IdentitySeedContributor>();
  }
}
