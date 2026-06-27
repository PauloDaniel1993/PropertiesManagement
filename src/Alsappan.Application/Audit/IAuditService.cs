using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Audit;

public interface IAuditService
{
  Task<ApplicationOperationResult<PagedResultDto<AuditEntryDto>>> ListAsync(
    AuditListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<AuditEntryDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<AuditCategoryLabelDto>> GetCategoryOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
