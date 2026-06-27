using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Vehicles;

public interface IVehicleService
{
  Task<ApplicationOperationResult<PagedResultDto<VehicleListItemDto>>> ListAsync(
    VehicleListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<VehicleDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<VehicleDetailDto>> CreateAsync(
    VehicleCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<VehicleDetailDto>> UpdateAsync(
    Guid id,
    VehicleUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<VehicleDetailDto>> AuthorizeAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<VehicleDetailDto>> DenyAsync(
    Guid id,
    VehicleLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<VehicleDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetAuthorizationStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
