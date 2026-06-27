using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Pets;

public sealed record PetListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  Guid? ResidentId = null,
  Guid? PropertyId = null,
  Guid? ContractId = null,
  string? Species = null,
  string? AuthorizationStatus = null,
  bool ActiveContractOnly = false,
  string? Sort = null,
  bool IncludeArchived = false,
  string? Locale = null);

public sealed record PetEntitySummaryDto(
  Guid Id,
  string Name,
  string? Description = null,
  string? Route = null);

public sealed record PetDocumentDto(
  Guid DocumentId,
  string Kind,
  string KindLabel,
  string? Label,
  string Route);

public sealed record PetAuthorizationHistoryItemDto(
  string Status,
  string StatusLabel,
  string? Notes,
  DateTimeOffset OccurredAt);

public sealed record PetListItemDto(
  Guid Id,
  string Name,
  StatusLabelDto Species,
  string? Breed,
  StatusLabelDto AuthorizationStatus,
  string? AuthorizationNotes,
  PetEntitySummaryDto Resident,
  PetEntitySummaryDto? Property,
  PetEntitySummaryDto? Contract,
  IReadOnlyList<PetDocumentDto> VaccinationRecordDocuments,
  IReadOnlyList<PetDocumentDto> AuthorizationFormDocuments,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  string? ConcurrencyToken);

public sealed record PetDetailDto(
  Guid Id,
  string Name,
  StatusLabelDto Species,
  string? Breed,
  StatusLabelDto AuthorizationStatus,
  string? AuthorizationNotes,
  string? Notes,
  PetEntitySummaryDto Resident,
  PetEntitySummaryDto? Property,
  PetEntitySummaryDto? Contract,
  IReadOnlyList<PetDocumentDto> VaccinationRecordDocuments,
  IReadOnlyList<PetDocumentDto> AuthorizationFormDocuments,
  IReadOnlyList<PetAuthorizationHistoryItemDto> AuthorizationHistory,
  string TimelineRoute,
  string AuditRoute,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record PetCreateRequestDto(
  Guid ResidentId,
  Guid? PropertyId,
  Guid? ContractId,
  string Name,
  string Species,
  string? Breed,
  string AuthorizationStatus,
  string? AuthorizationNotes,
  Guid? VaccinationRecordDocumentId,
  Guid? AuthorizationFormDocumentId,
  string? Notes);

public sealed record PetUpdateRequestDto(
  Guid ResidentId,
  Guid? PropertyId,
  Guid? ContractId,
  string Name,
  string Species,
  string? Breed,
  string AuthorizationStatus,
  string? AuthorizationNotes,
  Guid? VaccinationRecordDocumentId,
  Guid? AuthorizationFormDocumentId,
  string? Notes,
  string? ConcurrencyToken = null);

public sealed record PetLifecycleRequestDto(
  string? AuthorizationNotes = null);

public sealed record PetOptionsDto(
  IReadOnlyList<StatusLabelDto> Species,
  IReadOnlyList<StatusLabelDto> AuthorizationStatuses,
  IReadOnlyList<StatusLabelDto> DocumentKinds);
