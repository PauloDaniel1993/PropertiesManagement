using Alsappan.Application.ResidentPortal;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.ResidentPortal;

#pragma warning disable CA1812
internal sealed class ResidentPortalInfrastructureModule : IInfrastructureModule
{
  public int Order => 365;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IResidentPortalService, ResidentPortalService>();
  }
}
