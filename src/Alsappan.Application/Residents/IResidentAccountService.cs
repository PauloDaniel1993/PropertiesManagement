using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Residents;

public interface IResidentAccountService
{
  Task<ApplicationOperationResult<ResidentAccountAccessDto>> InviteAsync(
    Guid residentId,
    ResidentAccountInviteRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentAccountAccessDto>> ActivateAsync(
    Guid residentId,
    ResidentAccountActivateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentAccountAccessDto>> DeactivateAsync(
    Guid residentId,
    ResidentAccountLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentAccountAccessDto>> ResetPasswordAsync(
    Guid residentId,
    ResidentAccountPasswordResetRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentAccountAccessDto>> LinkAsync(
    Guid residentId,
    ResidentAccountLinkRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentAccountAccessDto>> UnlinkAsync(
    Guid residentId,
    ResidentAccountLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);
}
