using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Residents;

public sealed record ResidentListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Status = null,
  string? PortalStatus = null,
  bool? HasPortalAccess = null,
  string? Sort = null,
  bool IncludeArchived = false,
  string? Locale = null);

public sealed record ResidentEmergencyContactDto(
  string? Name,
  string? Relationship,
  string? Phone);

public sealed record ResidentListItemDto(
  Guid Id,
  string FullName,
  string? PreferredName,
  string? Email,
  string? Phone,
  string? SecondaryPhone,
  string? DocumentType,
  string? DocumentIdentifier,
  ResidentEmergencyContactDto EmergencyContact,
  StatusLabelDto Status,
  StatusLabelDto PortalStatus,
  IReadOnlyList<string> PrivacyFlags,
  string ContactSummary,
  bool IsSensitiveMasked,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt);

public sealed record ResidentDetailDto(
  Guid Id,
  string FullName,
  string? PreferredName,
  string? Email,
  string? Phone,
  string? SecondaryPhone,
  string? DocumentType,
  string? DocumentIdentifier,
  DateOnly? BirthDate,
  ResidentEmergencyContactDto EmergencyContact,
  StatusLabelDto Status,
  StatusLabelDto PortalStatus,
  IReadOnlyList<string> PrivacyFlags,
  string? Notes,
  Guid? LinkedUserId,
  string ContactSummary,
  bool IsSensitiveMasked,
  IReadOnlyList<ResidentRelationshipSummaryDto> Relationships,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record ResidentRelationshipSummaryDto(
  string Module,
  string Label,
  int Count,
  string Route);

public sealed record ResidentCreateRequestDto(
  string FullName,
  string? PreferredName,
  string? Email,
  string? Phone,
  string? SecondaryPhone,
  string? DocumentType,
  string? DocumentIdentifier,
  DateOnly? BirthDate,
  ResidentEmergencyContactDto? EmergencyContact,
  string Status,
  string PortalStatus,
  IReadOnlyList<string>? PrivacyFlags,
  string? Notes,
  Guid? LinkedUserId = null);

public sealed record ResidentUpdateRequestDto(
  string FullName,
  string? PreferredName,
  string? Email,
  string? Phone,
  string? SecondaryPhone,
  string? DocumentType,
  string? DocumentIdentifier,
  DateOnly? BirthDate,
  ResidentEmergencyContactDto? EmergencyContact,
  string Status,
  string PortalStatus,
  IReadOnlyList<string>? PrivacyFlags,
  string? Notes,
  Guid? LinkedUserId = null,
  string? ConcurrencyToken = null);

public sealed record ResidentDuplicateWarningRequestDto(
  string? Email,
  string? Phone,
  string? DocumentIdentifier,
  Guid? IgnoreResidentId = null);

public sealed record ResidentDuplicateWarningDto(
  string Field,
  string Value,
  Guid ResidentId,
  string ResidentName,
  string Message);
