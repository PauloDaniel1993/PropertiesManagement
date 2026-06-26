using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Infrastructure.Persistence;

public interface IActiveOrganizationContext
{
  OrganizationId? OrganizationId { get; }
}

public sealed class NoActiveOrganizationContext : IActiveOrganizationContext
{
  private NoActiveOrganizationContext()
  {
  }

  public static NoActiveOrganizationContext Instance { get; } = new();

  public OrganizationId? OrganizationId => null;
}

public sealed class StaticActiveOrganizationContext : IActiveOrganizationContext
{
  public StaticActiveOrganizationContext(OrganizationId organizationId)
  {
    OrganizationId = organizationId;
  }

  public OrganizationId? OrganizationId { get; }
}
