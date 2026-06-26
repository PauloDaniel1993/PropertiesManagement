namespace Alsappan.Application.Common.Tenancy;

public interface IActiveOrganizationSelectionProvider
{
  string? GetRequestedOrganizationId();
}
