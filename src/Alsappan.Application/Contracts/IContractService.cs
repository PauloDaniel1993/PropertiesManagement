using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Contracts;

public interface IContractService
{
  Task<ApplicationOperationResult<PagedResultDto<ContractListItemDto>>> ListAsync(
    ContractListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ContractDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ContractDetailDto>> CreateAsync(
    ContractCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ContractDetailDto>> UpdateAsync(
    Guid id,
    ContractUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ContractDetailDto>> ActivateAsync(
    Guid id,
    ContractLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ContractDetailDto>> TerminateAsync(
    Guid id,
    ContractLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ContractDetailDto>> CancelAsync(
    Guid id,
    ContractLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ContractDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetAdjustmentIndexOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
