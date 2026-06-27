using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Properties;

public sealed record PropertyListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Status = null,
  string? Type = null,
  bool? HasGarage = null,
  decimal? MinRent = null,
  decimal? MaxRent = null,
  string? Sort = null,
  bool IncludeArchived = false,
  string? Locale = null);

public sealed record PropertyAddressDto(
  string StreetLine,
  string Number,
  string? Complement,
  string Neighborhood,
  string City,
  string StateCode,
  string? PostalCode,
  string CountryCode = "BR");

public sealed record PropertyMoneyDto(
  decimal Amount,
  string Currency);

public sealed record PropertyListItemDto(
  Guid Id,
  string Name,
  string? Description,
  string Type,
  string TypeLabel,
  PropertyAddressDto Address,
  string Location,
  StatusLabelDto Status,
  PropertyMoneyDto SuggestedRent,
  int GarageSpaceCount,
  string? GarageSpaceIdentifiers,
  string GarageSummary,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt);

public sealed record PropertyDetailDto(
  Guid Id,
  string Name,
  string? Description,
  string Type,
  string TypeLabel,
  PropertyAddressDto Address,
  string Location,
  StatusLabelDto Status,
  PropertyMoneyDto SuggestedRent,
  int GarageSpaceCount,
  string? GarageSpaceIdentifiers,
  string GarageSummary,
  string? Notes,
  IReadOnlyList<PropertyRelationshipSummaryDto> Relationships,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record PropertyRelationshipSummaryDto(
  string Module,
  string Label,
  int Count,
  string Route);

public sealed record PropertyCreateRequestDto(
  string Name,
  string Type,
  string? Description,
  PropertyAddressDto Address,
  string Status,
  PropertyMoneyDto SuggestedRent,
  int GarageSpaceCount,
  string? GarageSpaceIdentifiers,
  string? Notes);

public sealed record PropertyUpdateRequestDto(
  string Name,
  string Type,
  string? Description,
  PropertyAddressDto Address,
  string Status,
  PropertyMoneyDto SuggestedRent,
  int GarageSpaceCount,
  string? GarageSpaceIdentifiers,
  string? Notes,
  string? ConcurrencyToken = null);

public sealed record PropertyStatusChangeRequestDto(
  string Status,
  string? Notes = null);
