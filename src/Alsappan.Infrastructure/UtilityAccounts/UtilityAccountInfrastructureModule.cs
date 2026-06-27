using Alsappan.Application.UtilityAccounts;
using Alsappan.Application.UtilityAccounts.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.UtilityAccounts;

#pragma warning disable CA1812
internal sealed class UtilityAccountInfrastructureModule : IInfrastructureModule
{
  public int Order => 360;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IUtilityAccountRepository, EfUtilityAccountRepository>();
    services.AddScoped<IUtilityAccountService, UtilityAccountService>();
  }
}
