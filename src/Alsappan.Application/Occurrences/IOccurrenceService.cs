using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Occurrences;

public interface IOccurrenceService
{
  Task<ApplicationOperationResult<PagedResultDto<OccurrenceListItemDto>>> ListAsync(
    OccurrenceListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> CreateAsync(
    OccurrenceCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> UpdateAsync(
    Guid id,
    OccurrenceUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> AssignAsync(
    Guid id,
    OccurrenceAssignmentRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> ChangePriorityAsync(
    Guid id,
    OccurrencePriorityChangeRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> ChangeStatusAsync(
    Guid id,
    OccurrenceStatusChangeRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> ResolveAsync(
    Guid id,
    OccurrenceResolutionRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> CancelAsync(
    Guid id,
    OccurrenceLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> AddCommentAsync(
    Guid id,
    OccurrenceCommentRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OccurrenceDetailDto>> AttachDocumentAsync(
    Guid id,
    OccurrenceAttachmentRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetPriorityOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
