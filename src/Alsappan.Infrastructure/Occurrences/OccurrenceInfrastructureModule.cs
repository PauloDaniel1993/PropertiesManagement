using Alsappan.Application.Occurrences;
using Alsappan.Application.Occurrences.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Occurrences;

#pragma warning disable CA1812
internal sealed class OccurrenceInfrastructureModule : IInfrastructureModule
{
  public int Order => 380;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IOccurrenceRepository, EfOccurrenceRepository>();
    services.AddScoped<IOccurrenceService, OccurrenceService>();
  }
}
