using Alsappan.Application.Search;
using Alsappan.Application.Search.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Search;

#pragma warning disable CA1812
internal sealed class SearchInfrastructureModule : IInfrastructureModule
{
  public int Order => 420;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IGlobalSearchRepository, EfGlobalSearchRepository>();
    services.AddScoped<IGlobalSearchService, GlobalSearchService>();
  }
}
