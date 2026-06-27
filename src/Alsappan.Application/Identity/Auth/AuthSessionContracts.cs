namespace Alsappan.Application.Identity.Auth;

public sealed record AuthSessionDto(
  string AccessToken,
  string TokenType,
  DateTimeOffset ExpiresAt,
  string RefreshToken,
  CurrentUserDto User);

public sealed record CurrentUserDto(
  Guid Id,
  string Email,
  string DisplayName,
  string AccountType,
  Guid? ActiveOrganizationId,
  IReadOnlyList<OrganizationContextDto> Organizations,
  IReadOnlyList<string> Permissions);

public sealed record OrganizationContextDto(
  Guid Id,
  string Slug,
  string Name,
  string DisplayName,
  string Locale,
  string CurrencyCode,
  IReadOnlyList<string> RoleCodes,
  IReadOnlyList<string> PermissionCodes);
