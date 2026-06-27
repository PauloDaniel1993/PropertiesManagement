using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Documents;

public sealed record DocumentListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Category = null,
  string? LinkedEntityType = null,
  Guid? LinkedEntityId = null,
  DateOnly? UploadedFrom = null,
  DateOnly? UploadedTo = null,
  Guid? UploadedByUserId = null,
  bool IncludeArchived = false,
  string? Locale = null);

public sealed record DocumentLinkRequestDto(
  string EntityType,
  Guid EntityId,
  string? Label = null);

public sealed record DocumentLinkDto(
  string EntityType,
  Guid EntityId,
  string? Label,
  string Route);

public sealed record DocumentVersionDto(
  Guid Id,
  int VersionNumber,
  string FileName,
  string ContentType,
  long SizeBytes,
  DateTimeOffset UploadedAt,
  Guid? UploadedByUserId,
  string? Notes);

public sealed record DocumentListItemDto(
  Guid Id,
  string Title,
  string? Description,
  string FileName,
  string ContentType,
  long SizeBytes,
  string Category,
  string CategoryLabel,
  StatusLabelDto Status,
  int CurrentVersionNumber,
  DateTimeOffset UploadedAt,
  DateTimeOffset? UpdatedAt,
  bool IsArchived,
  IReadOnlyList<DocumentLinkDto> Links,
  string? ConcurrencyToken);

public sealed record DocumentDetailDto(
  Guid Id,
  string Title,
  string? Description,
  string FileName,
  string ContentType,
  long SizeBytes,
  string Category,
  string CategoryLabel,
  StatusLabelDto Status,
  int CurrentVersionNumber,
  DateTimeOffset UploadedAt,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  IReadOnlyList<DocumentLinkDto> Links,
  IReadOnlyList<DocumentVersionDto> Versions,
  string DownloadRoute,
  string TimelineRoute,
  string AuditRoute,
  string? ConcurrencyToken);

public sealed record DocumentUploadRequestDto(
  string Category,
  string Title,
  string? Description,
  IReadOnlyList<DocumentLinkRequestDto> Links,
  string FileName,
  string ContentType,
  long SizeBytes,
  Stream Content,
  string? VersionNotes = null);

public sealed record DocumentUpdateRequestDto(
  string Category,
  string Title,
  string? Description,
  IReadOnlyList<DocumentLinkRequestDto> Links,
  string? ConcurrencyToken = null);

public sealed record DocumentVersionUploadRequestDto(
  string FileName,
  string ContentType,
  long SizeBytes,
  Stream Content,
  string? Notes = null);

public sealed record DocumentDownloadDto(
  string FileName,
  string ContentType,
  long SizeBytes,
  Stream Content);
