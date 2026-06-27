using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Payments;

namespace Alsappan.Application.Payments.Repositories;

public sealed record PaymentSnapshot(
  PaymentCharge Charge,
  PaymentContractSnapshot? Contract,
  PaymentPropertySnapshot? Property,
  PaymentResidentSnapshot? Resident,
  IReadOnlyList<PaymentReceiptSnapshot> Receipts);

public sealed record PaymentContractSnapshot(
  EntityId ContractId,
  EntityId PropertyId,
  EntityId PrimaryResidentId,
  string DisplayName,
  string PropertyName,
  string ResidentName,
  bool IsActive,
  Money MonthlyRent,
  int DueDay);

public sealed record PaymentPropertySnapshot(
  EntityId PropertyId,
  string Name,
  string? Location);

public sealed record PaymentResidentSnapshot(
  EntityId ResidentId,
  string Name);

public sealed record PaymentReceiptSnapshot(
  EntityId DocumentId,
  string? Label);

public interface IPaymentRepository
{
  Task<PagedResultDto<PaymentSnapshot>> ListAsync(
    PaymentListRequestDto request,
    OrganizationId organizationId,
    DateOnly today,
    CancellationToken cancellationToken = default);

  Task<PaymentCharge?> FindAsync(
    EntityId chargeId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<PaymentSnapshot?> FindSnapshotAsync(
    EntityId chargeId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<PaymentContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<PaymentPropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<PaymentResidentSnapshot?> GetResidentSnapshotAsync(
    EntityId residentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<bool> ReceiptDocumentExistsAsync(
    EntityId documentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<PaymentCharge?> FindByProviderReferenceAsync(
    string providerCode,
    string providerReference,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task AddAsync(PaymentCharge charge, CancellationToken cancellationToken = default);

  Task UpdateAsync(PaymentCharge charge, CancellationToken cancellationToken = default);
}
