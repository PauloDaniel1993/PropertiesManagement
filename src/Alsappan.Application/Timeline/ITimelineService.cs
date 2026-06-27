using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Timeline;

public interface ITimelineService
{
  Task<ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>> ListAsync(
    TimelineListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>> ListEntityAsync(
    string entityType,
    string entityId,
    TimelineEntityListRequestDto request,
    CancellationToken cancellationToken = default);
}
