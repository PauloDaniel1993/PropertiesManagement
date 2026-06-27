namespace Alsappan.Application.Identity.Auth;

public interface IIdentityAuthService
{
  Task<IdentityServiceResult<AuthSessionDto>> LoginAdminAsync(
    AdminLoginRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default);

  Task<IdentityServiceResult<AuthSessionDto>> LoginResidentAsync(
    ResidentLoginRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default);

  Task<IdentityServiceResult<AuthSessionDto>> RefreshAsync(
    RefreshAuthSessionRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default);

  Task<IdentityServiceResult> LogoutAsync(
    LogoutAuthSessionRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default);

  Task<IdentityServiceResult<CurrentUserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

  Task<IdentityServiceResult<AuthSessionDto>> SwitchOrganizationAsync(
    SwitchOrganizationRequest request,
    IdentityRequestContext? requestContext = null,
    CancellationToken cancellationToken = default);
}
