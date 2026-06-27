using Alsappan.Application.Residents;
using Alsappan.Application.Residents.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Residents;

#pragma warning disable CA1812
internal sealed class ResidentInfrastructureModule : IInfrastructureModule
{
  public int Order => 310;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IResidentRepository, EfResidentRepository>();
    services.AddScoped<IResidentService, ResidentService>();
  }
}
