namespace Alsappan.Application.Identity.Auth;

public sealed record AdminLoginRequest(
  string? Email,
  string? Password,
  Guid? OrganizationId = null);

public sealed record ResidentLoginRequest(
  string? Email,
  string? Password,
  Guid? OrganizationId = null);

public sealed record RefreshAuthSessionRequest(string? RefreshToken);

public sealed record LogoutAuthSessionRequest(string? RefreshToken);

public sealed record SwitchOrganizationRequest(Guid OrganizationId);

public sealed record IdentityRequestContext(
  string? UserAgent = null,
  string? IpAddress = null,
  string? CorrelationId = null,
  string? Locale = null);
