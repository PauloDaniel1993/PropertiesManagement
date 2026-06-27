using Alsappan.Application.Audit;
using Alsappan.Application.Audit.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Audit;

#pragma warning disable CA1812
internal sealed class AuditInfrastructureModule : IInfrastructureModule
{
  public int Order => 820;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IAuditRepository, EfAuditRepository>();
    services.AddScoped<IAuditService, AuditService>();
  }
}
