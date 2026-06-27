using Alsappan.Application.Common.Files;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Storage;

#pragma warning disable CA1812
internal sealed class StorageInfrastructureModule : IInfrastructureModule
{
  public int Order => 900;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IFileStorageProvider, LocalFileStorageProvider>();
  }
}
