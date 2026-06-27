using Alsappan.Application.Common.Seeding;
using Alsappan.Application.Settings;
using Alsappan.Application.Settings.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Settings;

#pragma warning disable CA1812
internal sealed class SettingsInfrastructureModule : IInfrastructureModule
{
  public int Order => 330;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<ISettingsRepository, EfSettingsRepository>();
    services.AddScoped<ISettingsService, SettingsService>();
    services.AddScoped<IDatabaseSeedContributor, SettingsSeedContributor>();
  }
}
