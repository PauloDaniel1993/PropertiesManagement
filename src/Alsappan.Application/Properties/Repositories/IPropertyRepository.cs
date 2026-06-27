using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Properties;

namespace Alsappan.Application.Properties.Repositories;

public interface IPropertyRepository
{
  Task<RentalProperty?> FindAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<PagedResultDto<RentalProperty>> ListAsync(
    PropertyListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task AddAsync(RentalProperty rentalProperty, CancellationToken cancellationToken = default);

  Task UpdateAsync(RentalProperty rentalProperty, CancellationToken cancellationToken = default);
}
