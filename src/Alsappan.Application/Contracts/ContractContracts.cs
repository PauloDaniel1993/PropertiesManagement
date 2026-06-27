using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Contracts;

public sealed record ContractListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Status = null,
  Guid? PropertyId = null,
  Guid? ResidentId = null,
  DateOnly? StartsFrom = null,
  DateOnly? StartsTo = null,
  DateOnly? EndsFrom = null,
  DateOnly? EndsTo = null,
  bool EndingSoonOnly = false,
  string? Sort = null,
  bool IncludeArchived = false,
  string? Locale = null);

public sealed record ContractMoneyDto(
  decimal Amount,
  string Currency);

public sealed record ContractPartyDto(
  Guid Id,
  string Name,
  bool IsPrimary);

public sealed record ContractPropertySummaryDto(
  Guid Id,
  string Name,
  string? Location);

public sealed record ContractListItemDto(
  Guid Id,
  ContractPropertySummaryDto Property,
  ContractPartyDto PrimaryResident,
  IReadOnlyList<ContractPartyDto> Residents,
  StatusLabelDto Status,
  DateOnly StartDate,
  DateOnly? EndDate,
  ContractMoneyDto MonthlyRent,
  int DueDay,
  string AdjustmentIndex,
  string AdjustmentIndexLabel,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  string? ConcurrencyToken);

public sealed record ContractDetailDto(
  Guid Id,
  ContractPropertySummaryDto Property,
  ContractPartyDto PrimaryResident,
  IReadOnlyList<ContractPartyDto> Residents,
  StatusLabelDto Status,
  DateOnly StartDate,
  DateOnly? EndDate,
  ContractMoneyDto MonthlyRent,
  int DueDay,
  ContractMoneyDto? DepositAmount,
  string AdjustmentIndex,
  string AdjustmentIndexLabel,
  int AdjustmentIntervalMonths,
  DateOnly? NextAdjustmentDate,
  string? PenaltyNotes,
  string? DiscountNotes,
  bool GeneratePaymentsAutomatically,
  string? Notes,
  IReadOnlyList<ContractRelationshipSummaryDto> Relationships,
  IReadOnlyList<ContractDocumentLinkSummaryDto> Documents,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record ContractRelationshipSummaryDto(
  string Module,
  string Label,
  int Count,
  string Route);

public sealed record ContractDocumentLinkSummaryDto(
  Guid? DocumentId,
  string Category,
  string Label,
  int Count,
  string Route);

public sealed record ContractCreateRequestDto(
  Guid PropertyId,
  Guid PrimaryResidentId,
  IReadOnlyList<Guid> ResidentIds,
  DateOnly StartDate,
  DateOnly? EndDate,
  ContractMoneyDto MonthlyRent,
  int DueDay,
  ContractMoneyDto? DepositAmount,
  string AdjustmentIndex,
  int AdjustmentIntervalMonths,
  DateOnly? NextAdjustmentDate,
  string? PenaltyNotes,
  string? DiscountNotes,
  bool GeneratePaymentsAutomatically,
  string? Notes,
  string LifecycleAction = "draft");

public sealed record ContractUpdateRequestDto(
  Guid PrimaryResidentId,
  IReadOnlyList<Guid> ResidentIds,
  DateOnly StartDate,
  DateOnly? EndDate,
  ContractMoneyDto MonthlyRent,
  int DueDay,
  ContractMoneyDto? DepositAmount,
  string AdjustmentIndex,
  int AdjustmentIntervalMonths,
  DateOnly? NextAdjustmentDate,
  string? PenaltyNotes,
  string? DiscountNotes,
  bool GeneratePaymentsAutomatically,
  string? Notes,
  string? ConcurrencyToken = null);

public sealed record ContractLifecycleRequestDto(
  DateOnly? EffectiveDate = null,
  string? Notes = null);
