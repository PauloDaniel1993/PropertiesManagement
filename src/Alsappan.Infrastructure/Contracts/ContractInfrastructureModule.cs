using Alsappan.Application.Contracts;
using Alsappan.Application.Contracts.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Contracts;

#pragma warning disable CA1812
internal sealed class ContractInfrastructureModule : IInfrastructureModule
{
  public int Order => 320;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IContractRepository, EfContractRepository>();
    services.AddScoped<IContractService, ContractService>();
  }
}
