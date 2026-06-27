using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Payments;

public sealed class EfPaymentRepository : IPaymentRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfPaymentRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<PaymentSnapshot>> ListAsync(
    PaymentListRequestDto request,
    OrganizationId organizationId,
    DateOnly today,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var listFilter = new ListFilterDto(
      request.Page,
      request.PageSize,
      request.Search,
      request.Sort,
      request.IncludeArchived);
    var query = BuildListQuery(request, organizationId, today);
    var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var rows = await ApplySort(query, request.Sort)
      .Skip(listFilter.Offset)
      .Take(listFilter.PageSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var snapshots = new List<PaymentSnapshot>(rows.Count);

    foreach (var charge in rows)
    {
      snapshots.Add(await BuildSnapshotAsync(charge, organizationId, cancellationToken).ConfigureAwait(false));
    }

    return new PagedResultDto<PaymentSnapshot>(snapshots, listFilter.Page, listFilter.PageSize, total);
  }

  public Task<PaymentCharge?> FindAsync(
    EntityId chargeId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = BaseQuery()
      .Where(charge => charge.Id == chargeId && charge.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(charge => charge.DeletedAt == null && charge.Status != PaymentStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<PaymentSnapshot?> FindSnapshotAsync(
    EntityId chargeId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var charge = await FindAsync(chargeId, organizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    return charge is null
      ? null
      : await BuildSnapshotAsync(charge, organizationId, cancellationToken).ConfigureAwait(false);
  }

  public async Task<PaymentContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var contract = await dbContext.Contracts.IgnoreQueryFilters()
      .AsNoTracking()
      .Where(candidate => candidate.Id == contractId && candidate.OrganizationId == organizationId)
      .Select(candidate => new
      {
        candidate.Id,
        candidate.PropertyId,
        candidate.PrimaryResidentId,
        candidate.Status,
        candidate.DeletedAt,
        candidate.StartDate,
        candidate.EndDate,
        candidate.MonthlyRent,
        candidate.DueDay
      })
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    if (contract is null)
    {
      return null;
    }

    var property = await GetPropertySnapshotAsync(contract.PropertyId, organizationId, cancellationToken)
      .ConfigureAwait(false);
    var resident = await GetResidentSnapshotAsync(contract.PrimaryResidentId, organizationId, cancellationToken)
      .ConfigureAwait(false);
    var propertyName = property?.Name ?? contract.PropertyId.Value.ToString("D");
    var residentName = resident?.Name ?? contract.PrimaryResidentId.Value.ToString("D");
    var displayName = $"Contrato {contract.StartDate:yyyy-MM} - {propertyName}";

    return new PaymentContractSnapshot(
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      displayName,
      propertyName,
      residentName,
      contract.DeletedAt is null && contract.Status == ContractStatus.Active,
      contract.MonthlyRent,
      contract.DueDay);
  }

  public async Task<PaymentPropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var property = await dbContext.Properties.IgnoreQueryFilters()
      .Where(candidate => candidate.Id == propertyId &&
        candidate.OrganizationId == organizationId &&
        candidate.DeletedAt == null &&
        candidate.Status != PropertyStatus.Archived)
      .Select(candidate => new
      {
        candidate.Id,
        candidate.Name,
        candidate.Address.StreetLine,
        candidate.Address.Number,
        candidate.Address.Neighborhood,
        candidate.Address.City,
        candidate.Address.StateCode
      })
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    return property is null
      ? null
      : new PaymentPropertySnapshot(
        property.Id,
        property.Name,
        $"{property.StreetLine}, {property.Number} - {property.Neighborhood}, {property.City}/{property.StateCode}");
  }

  public async Task<PaymentResidentSnapshot?> GetResidentSnapshotAsync(
    EntityId residentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var resident = await dbContext.Residents.IgnoreQueryFilters()
      .Where(candidate => candidate.Id == residentId &&
        candidate.OrganizationId == organizationId &&
        candidate.DeletedAt == null &&
        candidate.Status != ResidentStatus.Archived)
      .Select(candidate => new { candidate.Id, candidate.FullName })
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    return resident is null
      ? null
      : new PaymentResidentSnapshot(resident.Id, resident.FullName);
  }

  public Task<bool> ReceiptDocumentExistsAsync(
    EntityId documentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default) =>
    dbContext.Documents.IgnoreQueryFilters()
      .AnyAsync(document => document.Id == documentId &&
        document.OrganizationId == organizationId &&
        document.DeletedAt == null &&
        document.Status != DocumentStatus.Archived,
        cancellationToken);

  public Task<PaymentCharge?> FindByProviderReferenceAsync(
    string providerCode,
    string providerReference,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var normalizedProvider = PaymentCode.NormalizeCode(providerCode);
    return BaseQuery()
      .Where(charge => charge.OrganizationId == organizationId &&
        charge.ProviderCode == normalizedProvider &&
        charge.ProviderReference == providerReference &&
        charge.DeletedAt == null &&
        charge.Status != PaymentStatus.Archived)
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task AddAsync(PaymentCharge charge, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(charge);

    dbContext.PaymentCharges.Add(charge);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(PaymentCharge charge, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(charge);

    dbContext.PaymentCharges.Update(charge);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<PaymentCharge> BuildListQuery(
    PaymentListRequestDto request,
    OrganizationId organizationId,
    DateOnly today)
  {
    var query = BaseQuery()
      .Where(charge => charge.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(charge => charge.DeletedAt == null && charge.Status != PaymentStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = PaymentCode.NormalizeSearchText(request.Search);
      query = query.Where(charge => charge.SearchText.Contains(search));
    }

    if (PaymentCatalog.TryParseStatus(request.Status, out var status))
    {
      query = status switch
      {
        PaymentStatus.Overdue => query.Where(charge =>
          charge.Status == PaymentStatus.Pending && charge.DueDate < today),
        PaymentStatus.Pending => query.Where(charge =>
          charge.Status == PaymentStatus.Pending && charge.DueDate >= today),
        _ => query.Where(charge => charge.Status == status)
      };
    }

    if (request.ContractId.HasValue && request.ContractId.Value != Guid.Empty)
    {
      var contractId = new EntityId(request.ContractId.Value);
      query = query.Where(charge => charge.ContractId == contractId);
    }

    if (request.PropertyId.HasValue && request.PropertyId.Value != Guid.Empty)
    {
      var propertyId = new EntityId(request.PropertyId.Value);
      query = query.Where(charge => charge.PropertyId == propertyId);
    }

    if (request.ResidentId.HasValue && request.ResidentId.Value != Guid.Empty)
    {
      var residentId = new EntityId(request.ResidentId.Value);
      query = query.Where(charge => charge.ResidentId == residentId);
    }

    if (request.DueFrom.HasValue)
    {
      query = query.Where(charge => charge.DueDate >= request.DueFrom.Value);
    }

    if (request.DueTo.HasValue)
    {
      query = query.Where(charge => charge.DueDate <= request.DueTo.Value);
    }

    if (request.OverdueOnly)
    {
      query = query.Where(charge => charge.Status == PaymentStatus.Pending && charge.DueDate < today);
    }

    return query;
  }

  private static IQueryable<PaymentCharge> ApplySort(IQueryable<PaymentCharge> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return PaymentCode.NormalizeCode(string.IsNullOrWhiteSpace(key) ? "due-date" : key!) switch
    {
      "title" => descending
        ? query.OrderByDescending(charge => charge.Title).ThenBy(charge => charge.Id.Value)
        : query.OrderBy(charge => charge.Title).ThenBy(charge => charge.Id.Value),
      "status" => descending
        ? query.OrderByDescending(charge => charge.Status).ThenBy(charge => charge.DueDate)
        : query.OrderBy(charge => charge.Status).ThenBy(charge => charge.DueDate),
      "amount" or "gross-amount" => descending
        ? query.OrderByDescending(charge => charge.Amount.Amount).ThenBy(charge => charge.DueDate)
        : query.OrderBy(charge => charge.Amount.Amount).ThenBy(charge => charge.DueDate),
      "created-at" => descending
        ? query.OrderByDescending(charge => charge.CreatedAt).ThenBy(charge => charge.Id.Value)
        : query.OrderBy(charge => charge.CreatedAt).ThenBy(charge => charge.Id.Value),
      _ => descending
        ? query.OrderByDescending(charge => charge.DueDate).ThenBy(charge => charge.Id.Value)
        : query.OrderBy(charge => charge.DueDate).ThenBy(charge => charge.Id.Value)
    };
  }

  private async Task<PaymentSnapshot> BuildSnapshotAsync(
    PaymentCharge charge,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var contract = charge.ContractId.HasValue
      ? await GetContractSnapshotAsync(charge.ContractId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var propertyId = charge.PropertyId ?? contract?.PropertyId;
    var residentId = charge.ResidentId ?? contract?.PrimaryResidentId;
    var property = propertyId.HasValue
      ? await GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var resident = residentId.HasValue
      ? await GetResidentSnapshotAsync(residentId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var receipts = charge.ReceiptLinks
      .Where(link => link.DeletedAt is null)
      .OrderBy(link => link.CreatedAt)
      .Select(link => new PaymentReceiptSnapshot(link.DocumentId, link.Label))
      .ToArray();

    return new PaymentSnapshot(charge, contract, property, resident, receipts);
  }

  private IQueryable<PaymentCharge> BaseQuery() =>
    dbContext.PaymentCharges.IgnoreQueryFilters()
      .Include(charge => charge.Transactions)
      .Include(charge => charge.ReceiptLinks);
}
