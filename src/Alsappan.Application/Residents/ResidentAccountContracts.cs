using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Residents;

public sealed record ResidentAccountInviteRequestDto(
  string Email,
  string? DisplayName = null);

public sealed record ResidentAccountActivateRequestDto(
  string Password);

public sealed record ResidentAccountPasswordResetRequestDto(
  string Password);

public sealed record ResidentAccountLinkRequestDto(
  Guid? UserId = null,
  string? Email = null);

public sealed record ResidentAccountLifecycleRequestDto(
  string? Notes = null);

public sealed record ResidentAccountAccessDto(
  Guid ResidentId,
  Guid? UserId,
  string? Email,
  string? DisplayName,
  StatusLabelDto PortalStatus,
  bool HasPortalAccess,
  bool HasActiveAccountLink,
  string? InvitationToken,
  DateTimeOffset? UpdatedAt);
