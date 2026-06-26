namespace Alsappan.Application.Common.Tenancy;

public interface IActiveOrganizationContextResolver
{
  ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(CancellationToken cancellationToken = default);
}
