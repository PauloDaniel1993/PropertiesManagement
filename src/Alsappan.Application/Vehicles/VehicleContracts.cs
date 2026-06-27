using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Vehicles;

public sealed record VehicleListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Plate = null,
  Guid? ResidentId = null,
  Guid? PropertyId = null,
  Guid? ContractId = null,
  string? Type = null,
  string? AuthorizationStatus = null,
  bool? HasParkingAllocation = null,
  string? ParkingSpaceIdentifier = null,
  bool IncludeArchived = false,
  string? Sort = null,
  string? Locale = null);

public sealed record VehicleEntitySummaryDto(
  Guid Id,
  string Name,
  string? Description = null,
  string? Route = null);

public sealed record VehicleListItemDto(
  Guid Id,
  VehicleEntitySummaryDto Resident,
  VehicleEntitySummaryDto? Property,
  VehicleEntitySummaryDto? Contract,
  string Plate,
  string NormalizedPlate,
  StatusLabelDto Type,
  string? Color,
  string? Brand,
  string? Model,
  int? Year,
  StatusLabelDto AuthorizationStatus,
  string? ParkingSpaceIdentifier,
  string? NormalizedParkingSpaceIdentifier,
  string? ParkingAllocationNotes,
  bool HasParkingAllocation,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  string? ConcurrencyToken);

public sealed record VehicleDetailDto(
  Guid Id,
  VehicleEntitySummaryDto Resident,
  VehicleEntitySummaryDto? Property,
  VehicleEntitySummaryDto? Contract,
  string Plate,
  string NormalizedPlate,
  StatusLabelDto Type,
  string? Color,
  string? Brand,
  string? Model,
  int? Year,
  StatusLabelDto AuthorizationStatus,
  string? ParkingSpaceIdentifier,
  string? NormalizedParkingSpaceIdentifier,
  string? ParkingAllocationNotes,
  bool HasParkingAllocation,
  string? Notes,
  string TimelineRoute,
  string AuditRoute,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record VehicleCreateRequestDto(
  Guid? ResidentId,
  Guid? PropertyId,
  Guid? ContractId,
  string Plate,
  string Type,
  string? Color,
  string? Brand,
  string? Model,
  int? Year,
  string AuthorizationStatus,
  string? ParkingSpaceIdentifier,
  string? ParkingAllocationNotes,
  string? Notes);

public sealed record VehicleUpdateRequestDto(
  Guid? ResidentId,
  Guid? PropertyId,
  Guid? ContractId,
  string Plate,
  string Type,
  string? Color,
  string? Brand,
  string? Model,
  int? Year,
  string AuthorizationStatus,
  string? ParkingSpaceIdentifier,
  string? ParkingAllocationNotes,
  string? Notes,
  string? ConcurrencyToken = null);

public sealed record VehicleLifecycleRequestDto(string? Notes = null);
