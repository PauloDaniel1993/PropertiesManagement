using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Inspections;

public interface IInspectionService
{
  Task<ApplicationOperationResult<PagedResultDto<InspectionListItemDto>>> ListAsync(
    InspectionListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> ScheduleAsync(
    InspectionScheduleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> UpdateAsync(
    Guid id,
    InspectionUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> StartAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> CompleteAsync(
    Guid id,
    InspectionLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> CancelAsync(
    Guid id,
    InspectionLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> AddChecklistItemAsync(
    Guid inspectionId,
    InspectionChecklistItemRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> UpdateChecklistItemAsync(
    Guid inspectionId,
    Guid checklistItemId,
    InspectionChecklistItemRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> DeleteChecklistItemAsync(
    Guid inspectionId,
    Guid checklistItemId,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<InspectionDetailDto>> LinkDocumentAsync(
    Guid inspectionId,
    InspectionDocumentLinkRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetConditionRatingOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetDocumentKindOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
