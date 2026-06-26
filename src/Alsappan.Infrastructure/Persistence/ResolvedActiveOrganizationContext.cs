using Alsappan.Application.Common.Tenancy;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Infrastructure.Persistence;

public sealed class ResolvedActiveOrganizationContext : IActiveOrganizationContext
{
  private readonly IActiveOrganizationContextResolver resolver;
  private OrganizationId? organizationId;
  private bool resolved;

  public ResolvedActiveOrganizationContext(IActiveOrganizationContextResolver resolver)
  {
    this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
  }

  public OrganizationId? OrganizationId
  {
    get
    {
      if (!resolved)
      {
        var resolution = resolver.ResolveAsync().AsTask().GetAwaiter().GetResult();
        organizationId = resolution.Succeeded ? resolution.Context!.OrganizationId : null;
        resolved = true;
      }

      return organizationId;
    }
  }
}
