using Alsappan.Application.Pets;
using Alsappan.Application.Pets.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Pets;

#pragma warning disable CA1812
internal sealed class PetInfrastructureModule : IInfrastructureModule
{
  public int Order => 365;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IPetRepository, EfPetRepository>();
    services.AddScoped<IPetService, PetService>();
  }
}
