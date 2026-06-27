using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Payments;

namespace Alsappan.Application.ResidentPortal;

public sealed record ResidentPortalSettingsSummaryDto(
  bool IsEnabled,
  bool AllowOccurrenceCreation,
  bool AllowDocumentUpload,
  bool AllowProfileUpdateRequests);

public sealed record ResidentPortalProfileDto(
  Guid ResidentId,
  string FullName,
  string? PreferredName,
  string? Email,
  string? Phone,
  string? SecondaryPhone,
  StatusLabelDto Status,
  StatusLabelDto PortalStatus);

public sealed record ResidentPortalEntitySummaryDto(
  Guid Id,
  string Name,
  string? Description,
  string Route);

public sealed record ResidentPortalPropertyDto(
  Guid Id,
  string Name,
  string? Description,
  string StreetLine,
  string Number,
  string? Complement,
  string Neighborhood,
  string City,
  string StateCode,
  string? PostalCode,
  StatusLabelDto Status,
  PaymentMoneyDto SuggestedRent,
  int GarageSpaceCount);

public sealed record ResidentPortalContractDto(
  Guid Id,
  ResidentPortalEntitySummaryDto Property,
  StatusLabelDto Status,
  DateOnly StartDate,
  DateOnly? EndDate,
  PaymentMoneyDto MonthlyRent,
  int DueDay,
  bool IsArchived);

public sealed record ResidentPortalPaymentDto(
  Guid Id,
  string Title,
  string? Description,
  ResidentPortalEntitySummaryDto? Contract,
  ResidentPortalEntitySummaryDto? Property,
  DateOnly DueDate,
  StatusLabelDto Status,
  PaymentMoneyDto Amount,
  PaymentMoneyDto SettledAmount,
  PaymentMoneyDto Balance,
  string PreferredMethod,
  string PreferredMethodLabel,
  string? ProviderCode,
  string? ProviderReference,
  bool IsOverdue);

public sealed record ResidentPortalDocumentDto(
  Guid Id,
  string Title,
  string? Description,
  string FileName,
  string ContentType,
  long SizeBytes,
  string Category,
  string CategoryLabel,
  StatusLabelDto Status,
  DateTimeOffset UploadedAt,
  string DownloadRoute);

public sealed record ResidentPortalOccurrenceDto(
  Guid Id,
  string Title,
  string Description,
  StatusLabelDto Type,
  StatusLabelDto Priority,
  StatusLabelDto Status,
  ResidentPortalEntitySummaryDto? Property,
  ResidentPortalEntitySummaryDto? Contract,
  DateOnly? DueDate,
  bool IsUnresolved,
  DateTimeOffset CreatedAt);

public sealed record ResidentPortalInspectionDto(
  Guid Id,
  string Title,
  StatusLabelDto Type,
  StatusLabelDto Status,
  ResidentPortalEntitySummaryDto Property,
  ResidentPortalEntitySummaryDto? Contract,
  DateTimeOffset ScheduledAt,
  DateTimeOffset? CompletedAt,
  int TotalItems,
  int CompletedItems,
  decimal ProgressPercentage);

public sealed record ResidentPortalNotificationDto(
  Guid Id,
  StatusLabelDto Category,
  string Title,
  string Message,
  string? DeepLink,
  bool IsRead,
  DateTimeOffset CreatedAt);

public sealed record ResidentPortalSummaryDto(
  ResidentPortalProfileDto Profile,
  ResidentPortalPropertyDto? LinkedProperty,
  IReadOnlyList<ResidentPortalContractDto> Contracts,
  IReadOnlyList<ResidentPortalPaymentDto> Payments,
  IReadOnlyList<ResidentPortalDocumentDto> Documents,
  IReadOnlyList<ResidentPortalOccurrenceDto> Occurrences,
  IReadOnlyList<ResidentPortalInspectionDto> Inspections,
  IReadOnlyList<ResidentPortalNotificationDto> Notifications,
  ResidentPortalSettingsSummaryDto Settings);

public sealed record ResidentPortalOccurrenceCreateRequestDto(
  string Title,
  string Description,
  string Type,
  string Priority,
  Guid? PropertyId = null,
  Guid? ContractId = null,
  DateOnly? DueDate = null);

public sealed record ResidentPortalDocumentUploadRequestDto(
  string Category,
  string Title,
  string? Description,
  Guid? ContractId,
  Guid? PropertyId,
  string FileName,
  string ContentType,
  long SizeBytes,
  Stream Content,
  string? VersionNotes = null);
