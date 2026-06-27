using Alsappan.Application.Common.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Alsappan.Application.Payments;

public sealed record PaymentListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Status = null,
  Guid? ContractId = null,
  Guid? PropertyId = null,
  Guid? ResidentId = null,
  DateOnly? DueFrom = null,
  DateOnly? DueTo = null,
  bool OverdueOnly = false,
  string? Sort = null,
  bool IncludeArchived = false,
  string? Locale = null);

public sealed record PaymentMoneyDto(
  decimal Amount,
  string Currency);

public sealed record PaymentEntitySummaryDto(
  Guid Id,
  string Name,
  string? Description = null,
  string? Route = null);

public sealed record PaymentTransactionDto(
  Guid Id,
  PaymentMoneyDto Amount,
  string Method,
  string MethodLabel,
  DateOnly SettledOn,
  string? BankReference,
  string? ProviderCode,
  string? ProviderReference,
  Guid? ReceiptDocumentId,
  string? Notes,
  bool IsReversed,
  DateTimeOffset CreatedAt);

public sealed record PaymentReceiptDocumentDto(
  Guid DocumentId,
  string? Label,
  string Route);

public sealed record PaymentListItemDto(
  Guid Id,
  string Title,
  string? Description,
  PaymentEntitySummaryDto? Contract,
  PaymentEntitySummaryDto? Property,
  PaymentEntitySummaryDto? Resident,
  DateOnly DueDate,
  StatusLabelDto Status,
  PaymentMoneyDto Amount,
  PaymentMoneyDto DiscountAmount,
  PaymentMoneyDto PenaltyAmount,
  PaymentMoneyDto GrossAmount,
  PaymentMoneyDto SettledAmount,
  PaymentMoneyDto Balance,
  string PreferredMethod,
  string PreferredMethodLabel,
  bool IsOverdue,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  string? ConcurrencyToken);

public sealed record PaymentDetailDto(
  Guid Id,
  string Title,
  string? Description,
  PaymentEntitySummaryDto? Contract,
  PaymentEntitySummaryDto? Property,
  PaymentEntitySummaryDto? Resident,
  Guid? UtilityAccountId,
  DateOnly DueDate,
  StatusLabelDto Status,
  PaymentMoneyDto Amount,
  PaymentMoneyDto DiscountAmount,
  PaymentMoneyDto PenaltyAmount,
  PaymentMoneyDto GrossAmount,
  PaymentMoneyDto SettledAmount,
  PaymentMoneyDto Balance,
  string PreferredMethod,
  string PreferredMethodLabel,
  StatusLabelDto ReconciliationStatus,
  string? ProviderCode,
  string? ProviderReference,
  string? ProviderMetadataJson,
  string? Notes,
  IReadOnlyList<PaymentTransactionDto> Transactions,
  IReadOnlyList<PaymentReceiptDocumentDto> ReceiptDocuments,
  string TimelineRoute,
  string AuditRoute,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record PaymentCreateRequestDto(
  string Title,
  string? Description,
  Guid? ContractId,
  Guid? PropertyId,
  Guid? ResidentId,
  Guid? UtilityAccountId,
  DateOnly DueDate,
  PaymentMoneyDto Amount,
  PaymentMoneyDto? DiscountAmount,
  PaymentMoneyDto? PenaltyAmount,
  string PreferredMethod,
  string ReconciliationStatus,
  string? Notes);

public sealed record PaymentUpdateRequestDto(
  string Title,
  string? Description,
  Guid? ContractId,
  Guid? PropertyId,
  Guid? ResidentId,
  Guid? UtilityAccountId,
  DateOnly DueDate,
  PaymentMoneyDto Amount,
  PaymentMoneyDto? DiscountAmount,
  PaymentMoneyDto? PenaltyAmount,
  string PreferredMethod,
  string ReconciliationStatus,
  string? Notes,
  string? ConcurrencyToken = null);

public sealed record PaymentTransactionRequestDto(
  PaymentMoneyDto Amount,
  string Method,
  DateOnly SettledOn,
  string? BankReference,
  string? ProviderCode,
  string? ProviderReference,
  Guid? ReceiptDocumentId,
  string? Notes);

public sealed record PaymentTransactionReversalRequestDto(
  Guid TransactionId,
  string? Notes = null);

public sealed record PaymentLifecycleRequestDto(
  string? Notes = null);

public sealed record PaymentInstructionRequestDto(
  string ProviderCode);

[SuppressMessage(
  "Design",
  "CA1054:URI-like parameters should not be strings",
  Justification = "Payment provider DTOs expose URLs as JSON strings for frontend and OpenAPI compatibility.")]
[SuppressMessage(
  "Design",
  "CA1056:URI-like properties should not be strings",
  Justification = "Payment provider DTOs expose URLs as JSON strings for frontend and OpenAPI compatibility.")]
public sealed record PaymentInstructionDto(
  Guid ChargeId,
  string ProviderCode,
  string ProviderReference,
  string Kind,
  string Status,
  PaymentMoneyDto Amount,
  DateOnly DueDate,
  DateTimeOffset? ExpiresAt,
  string PayerSummary,
  string? Barcode,
  string? LinhaDigitavel,
  string? QrPayload,
  string? CopyPasteCode,
  string? PaymentIntentId,
  string? ApprovalUrl,
  IReadOnlyDictionary<string, string> Metadata);

public sealed record PaymentProviderEventRequestDto(
  string ProviderCode,
  string ProviderReference,
  string EventType,
  DateOnly SettledOn,
  PaymentMoneyDto? Amount = null,
  string? Notes = null);
