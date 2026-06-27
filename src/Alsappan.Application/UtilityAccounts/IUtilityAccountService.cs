using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.UtilityAccounts;

public interface IUtilityAccountService
{
  Task<ApplicationOperationResult<PagedResultDto<UtilityAccountListItemDto>>> ListAsync(
    UtilityAccountListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<UtilityAccountDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<UtilityAccountDetailDto>> CreateAsync(
    UtilityAccountCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<UtilityAccountDetailDto>> UpdateAsync(
    Guid id,
    UtilityAccountUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<UtilityAccountDetailDto>> MarkPaidAsync(
    Guid id,
    UtilityMarkPaidRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<UtilityAccountDetailDto>> CancelAsync(
    Guid id,
    UtilityLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<UtilityAccountDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetResponsibilityOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
