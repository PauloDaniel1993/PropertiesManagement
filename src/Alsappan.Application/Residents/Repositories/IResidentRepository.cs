using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Residents;

namespace Alsappan.Application.Residents.Repositories;

public interface IResidentRepository
{
  Task<Resident?> FindAsync(
    EntityId residentId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<PagedResultDto<Resident>> ListAsync(
    ResidentListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Resident>> FindPotentialDuplicatesAsync(
    ResidentDuplicateWarningRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task AddAsync(Resident resident, CancellationToken cancellationToken = default);

  Task UpdateAsync(Resident resident, CancellationToken cancellationToken = default);
}
