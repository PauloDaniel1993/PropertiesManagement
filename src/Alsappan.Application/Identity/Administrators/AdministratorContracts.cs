using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Identity.Administrators;

public sealed record AdministratorListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Status = null,
  string? Role = null,
  bool IncludeArchived = false);

public sealed record AdministratorListItemDto(
  Guid Id,
  string Email,
  string DisplayName,
  string Status,
  IReadOnlyList<string> RoleCodes,
  DateTimeOffset CreatedAt,
  DateTimeOffset? LastLoginAt);

public sealed record AdministratorDetailDto(
  Guid Id,
  string Email,
  string DisplayName,
  string Status,
  IReadOnlyList<string> RoleCodes,
  IReadOnlyList<string> PermissionCodes,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? LastLoginAt,
  string? ConcurrencyToken);

public sealed record AdministratorCreateRequestDto(
  string Email,
  string DisplayName,
  IReadOnlyList<string> RoleCodes,
  string? TemporaryPassword = null);

public sealed record AdministratorUpdateRequestDto(
  string Email,
  string DisplayName,
  IReadOnlyList<string> RoleCodes,
  string? ConcurrencyToken = null);
