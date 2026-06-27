using Alsappan.Application.Common.Contracts;
using Alsappan.Application.UtilityAccounts;
using Alsappan.Application.UtilityAccounts.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Domain.UtilityAccounts;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.UtilityAccounts;

public sealed class EfUtilityAccountRepository : IUtilityAccountRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfUtilityAccountRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<UtilityAccountSnapshot>> ListAsync(
    UtilityAccountListRequestDto request,
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
    var snapshots = new List<UtilityAccountSnapshot>(rows.Count);

    foreach (var account in rows)
    {
      snapshots.Add(await BuildSnapshotAsync(account, organizationId, cancellationToken).ConfigureAwait(false));
    }

    return new PagedResultDto<UtilityAccountSnapshot>(snapshots, listFilter.Page, listFilter.PageSize, total);
  }

  public Task<UtilityAccount?> FindAsync(
    EntityId utilityAccountId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = BaseQuery()
      .Where(account => account.Id == utilityAccountId && account.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(account => account.DeletedAt == null && account.Status != UtilityAccountStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<UtilityAccountSnapshot?> FindSnapshotAsync(
    EntityId utilityAccountId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var account = await FindAsync(utilityAccountId, organizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    return account is null
      ? null
      : await BuildSnapshotAsync(account, organizationId, cancellationToken).ConfigureAwait(false);
  }

  public async Task<UtilityContractSnapshot?> GetContractSnapshotAsync(
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
        candidate.StartDate
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

    return new UtilityContractSnapshot(
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      displayName,
      propertyName,
      residentName,
      contract.DeletedAt is null && contract.Status == ContractStatus.Active);
  }

  public async Task<UtilityPropertySnapshot?> GetPropertySnapshotAsync(
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
      : new UtilityPropertySnapshot(
        property.Id,
        property.Name,
        $"{property.StreetLine}, {property.Number} - {property.Neighborhood}, {property.City}/{property.StateCode}");
  }

  public async Task<UtilityResidentSnapshot?> GetResidentSnapshotAsync(
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
      : new UtilityResidentSnapshot(resident.Id, resident.FullName);
  }

  public Task<bool> DocumentExistsAsync(
    EntityId documentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default) =>
    dbContext.Documents.IgnoreQueryFilters()
      .AnyAsync(document => document.Id == documentId &&
        document.OrganizationId == organizationId &&
        document.DeletedAt == null &&
        document.Status != DocumentStatus.Archived,
        cancellationToken);

  public async Task AddAsync(UtilityAccount utilityAccount, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(utilityAccount);

    dbContext.UtilityAccounts.Add(utilityAccount);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(UtilityAccount utilityAccount, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(utilityAccount);

    dbContext.UtilityAccounts.Update(utilityAccount);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<UtilityAccount> BuildListQuery(
    UtilityAccountListRequestDto request,
    OrganizationId organizationId,
    DateOnly today)
  {
    var query = BaseQuery()
      .Where(account => account.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(account => account.DeletedAt == null && account.Status != UtilityAccountStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = UtilityAccountCode.NormalizeSearchText(request.Search);
      query = query.Where(account => account.SearchText.Contains(search));
    }

    if (UtilityAccountCatalog.TryParseType(request.Type, out var type))
    {
      query = query.Where(account => account.Type == type);
    }

    if (UtilityAccountCatalog.TryParseResponsibility(request.Responsibility, out var responsibility))
    {
      query = query.Where(account => account.Responsibility == responsibility);
    }

    if (UtilityAccountCatalog.TryParseStatus(request.Status, out var status))
    {
      query = status switch
      {
        UtilityAccountStatus.Overdue => query.Where(account =>
          account.DeletedAt == null && account.Status == UtilityAccountStatus.Open && account.DueDate < today),
        UtilityAccountStatus.Open => query.Where(account =>
          account.DeletedAt == null && account.Status == UtilityAccountStatus.Open && account.DueDate >= today),
        UtilityAccountStatus.Archived => query.Where(account =>
          account.DeletedAt != null || account.Status == UtilityAccountStatus.Archived),
        _ => query.Where(account => account.DeletedAt == null && account.Status == status)
      };
    }

    if (request.PropertyId.HasValue && request.PropertyId.Value != Guid.Empty)
    {
      var propertyId = new EntityId(request.PropertyId.Value);
      query = query.Where(account => account.PropertyId == propertyId);
    }

    if (request.ContractId.HasValue && request.ContractId.Value != Guid.Empty)
    {
      var contractId = new EntityId(request.ContractId.Value);
      query = query.Where(account => account.ContractId == contractId);
    }

    if (request.BillingFrom.HasValue)
    {
      query = query.Where(account => account.BillingPeriodEnd >= request.BillingFrom.Value);
    }

    if (request.BillingTo.HasValue)
    {
      query = query.Where(account => account.BillingPeriodStart <= request.BillingTo.Value);
    }

    if (request.DueFrom.HasValue)
    {
      query = query.Where(account => account.DueDate >= request.DueFrom.Value);
    }

    if (request.DueTo.HasValue)
    {
      query = query.Where(account => account.DueDate <= request.DueTo.Value);
    }

    if (request.OverdueOnly)
    {
      query = query.Where(account => account.Status == UtilityAccountStatus.Open && account.DueDate < today);
    }

    return query;
  }

  private static IQueryable<UtilityAccount> ApplySort(IQueryable<UtilityAccount> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return NormalizeSortKey(string.IsNullOrWhiteSpace(key) ? "due-date" : key!) switch
    {
      "TITLE" => descending
        ? query.OrderByDescending(account => account.Title).ThenBy(account => account.Id.Value)
        : query.OrderBy(account => account.Title).ThenBy(account => account.Id.Value),
      "STATUS" => descending
        ? query.OrderByDescending(account => account.Status).ThenBy(account => account.DueDate)
        : query.OrderBy(account => account.Status).ThenBy(account => account.DueDate),
      "AMOUNT" => descending
        ? query.OrderByDescending(account => account.Amount.Amount).ThenBy(account => account.DueDate)
        : query.OrderBy(account => account.Amount.Amount).ThenBy(account => account.DueDate),
      "CREATED-AT" => descending
        ? query.OrderByDescending(account => account.CreatedAt).ThenBy(account => account.Id.Value)
        : query.OrderBy(account => account.CreatedAt).ThenBy(account => account.Id.Value),
      _ => descending
        ? query.OrderByDescending(account => account.DueDate).ThenBy(account => account.Id.Value)
        : query.OrderBy(account => account.DueDate).ThenBy(account => account.Id.Value)
    };
  }

  private async Task<UtilityAccountSnapshot> BuildSnapshotAsync(
    UtilityAccount account,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var contract = account.ContractId.HasValue
      ? await GetContractSnapshotAsync(account.ContractId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var propertyId = account.PropertyId ?? contract?.PropertyId;
    var residentId = account.ResidentId ?? contract?.PrimaryResidentId;
    var property = propertyId.HasValue
      ? await GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var resident = residentId.HasValue
      ? await GetResidentSnapshotAsync(residentId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var documents = account.DocumentLinks
      .Where(link => link.DeletedAt is null)
      .OrderBy(link => link.Kind)
      .ThenBy(link => link.CreatedAt)
      .Select(link => new UtilityDocumentSnapshot(link.DocumentId, link.Kind, link.Label))
      .ToArray();

    return new UtilityAccountSnapshot(account, property, contract, resident, documents);
  }

  private IQueryable<UtilityAccount> BaseQuery() =>
    dbContext.UtilityAccounts.IgnoreQueryFilters()
      .Include(account => account.DocumentLinks);

  private static string NormalizeSortKey(string value) =>
    value.Trim().ToUpperInvariant();
}
