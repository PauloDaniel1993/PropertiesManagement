using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Residents;

public interface IResidentService
{
  Task<ApplicationOperationResult<PagedResultDto<ResidentListItemDto>>> ListAsync(
    ResidentListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentDetailDto>> CreateAsync(
    ResidentCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentDetailDto>> UpdateAsync(
    Guid id,
    ResidentUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<IReadOnlyList<ResidentDuplicateWarningDto>>> GetDuplicateWarningsAsync(
    ResidentDuplicateWarningRequestDto request,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetPortalStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetPrivacyFlagOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
