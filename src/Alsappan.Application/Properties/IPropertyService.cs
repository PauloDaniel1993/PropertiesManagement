using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Properties;

public interface IPropertyService
{
  Task<ApplicationOperationResult<PagedResultDto<PropertyListItemDto>>> ListAsync(
    PropertyListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PropertyDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PropertyDetailDto>> CreateAsync(
    PropertyCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PropertyDetailDto>> UpdateAsync(
    Guid id,
    PropertyUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PropertyDetailDto>> ChangeStatusAsync(
    Guid id,
    PropertyStatusChangeRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<PropertyDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
