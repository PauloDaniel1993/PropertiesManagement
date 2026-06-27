using Alsappan.Application.Vehicles;
using Alsappan.Application.Vehicles.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Vehicles;

#pragma warning disable CA1812
internal sealed class VehicleInfrastructureModule : IInfrastructureModule
{
  public int Order => 370;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IVehicleRepository, EfVehicleRepository>();
    services.AddScoped<IVehicleService, VehicleService>();
  }
}
