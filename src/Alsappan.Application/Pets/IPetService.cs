using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Pets;

public interface IPetService
{
  Task<ApplicationOperationResult<PagedResultDto<PetListItemDto>>> ListAsync(
    PetListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PetDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PetDetailDto>> CreateAsync(
    PetCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PetDetailDto>> UpdateAsync(
    Guid id,
    PetUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PetDetailDto>> AuthorizeAsync(
    Guid id,
    PetLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PetDetailDto>> DenyAsync(
    Guid id,
    PetLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PetDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<PetOptionsDto> GetOptionsAsync(string? locale = null, CancellationToken cancellationToken = default);
}
