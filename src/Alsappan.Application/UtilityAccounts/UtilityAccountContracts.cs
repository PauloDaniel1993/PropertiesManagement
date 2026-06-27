using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.UtilityAccounts;

public sealed record UtilityAccountListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Type = null,
  string? Status = null,
  string? Responsibility = null,
  Guid? PropertyId = null,
  Guid? ContractId = null,
  DateOnly? BillingFrom = null,
  DateOnly? BillingTo = null,
  DateOnly? DueFrom = null,
  DateOnly? DueTo = null,
  bool OverdueOnly = false,
  string? Sort = null,
  bool IncludeArchived = false,
  string? Locale = null);

public sealed record UtilityMoneyDto(
  decimal Amount,
  string Currency);

public sealed record UtilityEntitySummaryDto(
  Guid Id,
  string Name,
  string? Description = null,
  string? Route = null);

public sealed record UtilityDocumentDto(
  Guid DocumentId,
  string Kind,
  string KindLabel,
  string? Label,
  string Route);

public sealed record UtilityAccountListItemDto(
  Guid Id,
  string Title,
  string? Description,
  StatusLabelDto Type,
  StatusLabelDto Status,
  StatusLabelDto Responsibility,
  UtilityEntitySummaryDto? Property,
  UtilityEntitySummaryDto? Contract,
  UtilityEntitySummaryDto? Resident,
  DateOnly BillingPeriodStart,
  DateOnly BillingPeriodEnd,
  DateOnly DueDate,
  UtilityMoneyDto Amount,
  UtilityMoneyDto PaidAmount,
  UtilityMoneyDto Balance,
  DateOnly? PaidOn,
  string? PaymentMethod,
  string? BankReference,
  bool IsOverdue,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  string? ConcurrencyToken);

public sealed record UtilityAccountDetailDto(
  Guid Id,
  string Title,
  string? Description,
  StatusLabelDto Type,
  StatusLabelDto Status,
  StatusLabelDto Responsibility,
  UtilityEntitySummaryDto? Property,
  UtilityEntitySummaryDto? Contract,
  UtilityEntitySummaryDto? Resident,
  DateOnly BillingPeriodStart,
  DateOnly BillingPeriodEnd,
  DateOnly DueDate,
  UtilityMoneyDto Amount,
  UtilityMoneyDto PaidAmount,
  UtilityMoneyDto Balance,
  DateOnly? PaidOn,
  string? PaymentMethod,
  string? BankReference,
  string? Notes,
  IReadOnlyList<UtilityDocumentDto> BillDocuments,
  IReadOnlyList<UtilityDocumentDto> ReceiptDocuments,
  string TimelineRoute,
  string AuditRoute,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record UtilityAccountCreateRequestDto(
  string Title,
  string? Description,
  string Type,
  string Responsibility,
  Guid? PropertyId,
  Guid? ContractId,
  Guid? ResidentId,
  DateOnly BillingPeriodStart,
  DateOnly BillingPeriodEnd,
  DateOnly DueDate,
  UtilityMoneyDto Amount,
  Guid? BillDocumentId,
  string? Notes);

public sealed record UtilityAccountUpdateRequestDto(
  string Title,
  string? Description,
  string Type,
  string Responsibility,
  Guid? PropertyId,
  Guid? ContractId,
  Guid? ResidentId,
  DateOnly BillingPeriodStart,
  DateOnly BillingPeriodEnd,
  DateOnly DueDate,
  UtilityMoneyDto Amount,
  Guid? BillDocumentId,
  string? Notes,
  string? ConcurrencyToken = null);

public sealed record UtilityMarkPaidRequestDto(
  UtilityMoneyDto Amount,
  DateOnly PaidOn,
  string? PaymentMethod,
  string? BankReference,
  Guid? ReceiptDocumentId,
  string? Notes);

public sealed record UtilityLifecycleRequestDto(
  string? Notes = null);
