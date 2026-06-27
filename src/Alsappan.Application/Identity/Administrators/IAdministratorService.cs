using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Identity;

namespace Alsappan.Application.Identity.Administrators;

public interface IAdministratorService
{
  Task<IdentityOperationResult<PagedResultDto<AdministratorListItemDto>>> ListAsync(
    AdministratorListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<IdentityOperationResult<AdministratorDetailDto>> GetAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<IdentityOperationResult<AdministratorDetailDto>> CreateAsync(
    AdministratorCreateRequestDto request,
    CancellationToken cancellationToken = default);

  Task<IdentityOperationResult<AdministratorDetailDto>> UpdateAsync(
    Guid id,
    AdministratorUpdateRequestDto request,
    CancellationToken cancellationToken = default);

  Task<IdentityOperationResult<AdministratorDetailDto>> DeactivateAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<IdentityOperationResult<AdministratorDetailDto>> ReactivateAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<IdentityOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetRoleOptionsAsync(
    CancellationToken cancellationToken = default);
}
