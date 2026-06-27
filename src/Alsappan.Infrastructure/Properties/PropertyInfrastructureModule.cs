using Alsappan.Application.Properties;
using Alsappan.Application.Properties.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Properties;

#pragma warning disable CA1812
internal sealed class PropertyInfrastructureModule : IInfrastructureModule
{
  public int Order => 300;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IPropertyRepository, EfPropertyRepository>();
    services.AddScoped<IPropertyService, PropertyService>();
  }
}
