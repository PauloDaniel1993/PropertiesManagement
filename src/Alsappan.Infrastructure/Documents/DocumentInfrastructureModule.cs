using Alsappan.Application.Documents;
using Alsappan.Application.Documents.Repositories;
using Alsappan.Infrastructure.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Documents;

#pragma warning disable CA1812
internal sealed class DocumentInfrastructureModule : IInfrastructureModule
{
  public int Order => 330;

  public void AddServices(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddScoped<IDocumentRepository, EfDocumentRepository>();
    services.AddScoped<IDocumentService, DocumentService>();
  }
}
