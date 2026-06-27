using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Contracts;
using Alsappan.Application.Contracts.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Contracts;

public sealed class EfContractRepository : IContractRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfContractRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<ContractSnapshot?> FindSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var contract = await FindAsync(contractId, organizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);

    return contract is null
      ? null
      : await BuildSnapshotAsync(contract, organizationId, cancellationToken).ConfigureAwait(false);
  }

  public Task<LeaseContract?> FindAsync(
    EntityId contractId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Contracts.IgnoreQueryFilters()
      .Include(contract => contract.Residents)
      .Where(contract => contract.Id == contractId && contract.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(contract => contract.DeletedAt == null && contract.Status != ContractStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<PagedResultDto<ContractSnapshot>> ListAsync(
    ContractListRequestDto request,
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
    var snapshots = new List<ContractSnapshot>(rows.Count);

    foreach (var row in rows)
    {
      snapshots.Add(await BuildSnapshotAsync(row, organizationId, cancellationToken).ConfigureAwait(false));
    }

    return new PagedResultDto<ContractSnapshot>(snapshots, listFilter.Page, listFilter.PageSize, total);
  }

  public async Task<ContractPropertySnapshot?> GetPropertySnapshotAsync(
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
      : new ContractPropertySnapshot(
        property.Id,
        property.Name,
        $"{property.StreetLine}, {property.Number} - {property.Neighborhood}, {property.City}/{property.StateCode}");
  }

  public async Task<IReadOnlyList<ContractResidentSnapshot>> GetResidentSnapshotsAsync(
    IReadOnlyCollection<EntityId> residentIds,
    EntityId primaryResidentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(residentIds);

    if (residentIds.Count == 0)
    {
      return [];
    }

    var residents = await dbContext.Residents.IgnoreQueryFilters()
      .Where(resident => resident.OrganizationId == organizationId &&
        residentIds.Contains(resident.Id) &&
        resident.DeletedAt == null &&
        resident.Status != ResidentStatus.Archived)
      .OrderBy(resident => resident.FullName)
      .Select(resident => new { resident.Id, resident.FullName })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return residents
      .Select(resident => new ContractResidentSnapshot(
        resident.Id,
        resident.FullName,
        resident.Id == primaryResidentId))
      .ToArray();
  }

  public Task<bool> HasOverlappingActiveContractAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    DateOnly startDate,
    DateOnly? endDate,
    EntityId? ignoredContractId = null,
    CancellationToken cancellationToken = default)
  {
    var normalizedEnd = endDate ?? DateOnly.MaxValue;
    var query = dbContext.Contracts.IgnoreQueryFilters()
      .Where(contract => contract.OrganizationId == organizationId &&
        contract.PropertyId == propertyId &&
        contract.Status == ContractStatus.Active &&
        contract.DeletedAt == null &&
        contract.StartDate <= normalizedEnd &&
        (contract.EndDate == null || contract.EndDate >= startDate));

    if (ignoredContractId.HasValue)
    {
      query = query.Where(contract => contract.Id != ignoredContractId.Value);
    }

    return query.AnyAsync(cancellationToken);
  }

  public Task<bool> HasAnyActiveContractAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    EntityId? ignoredContractId = null,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Contracts.IgnoreQueryFilters()
      .Where(contract => contract.OrganizationId == organizationId &&
        contract.PropertyId == propertyId &&
        contract.Status == ContractStatus.Active &&
        contract.DeletedAt == null);

    if (ignoredContractId.HasValue)
    {
      query = query.Where(contract => contract.Id != ignoredContractId.Value);
    }

    return query.AnyAsync(cancellationToken);
  }

  public async Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(leaseContract);

    dbContext.Contracts.Add(leaseContract);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(leaseContract);

    dbContext.Contracts.Update(leaseContract);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<LeaseContract> BuildListQuery(
    ContractListRequestDto request,
    OrganizationId organizationId,
    DateOnly today)
  {
    var query = dbContext.Contracts.IgnoreQueryFilters()
      .Include(contract => contract.Residents)
      .Where(contract => contract.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(contract => contract.DeletedAt == null && contract.Status != ContractStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = ContractCode.NormalizeSearchText(request.Search);
      query = query.Where(contract => contract.SearchText.Contains(search));
    }

    if (ContractCatalog.TryParseStatus(request.Status, out var status))
    {
      query = status switch
      {
        ContractStatus.EndingSoon => query.Where(contract =>
          contract.Status == ContractStatus.Active &&
          contract.EndDate != null &&
          contract.EndDate >= today &&
          contract.EndDate <= today.AddDays(LeaseContract.DefaultEndingSoonDays)),
        ContractStatus.Ended => query.Where(contract =>
          contract.Status == ContractStatus.Active &&
          contract.EndDate != null &&
          contract.EndDate < today),
        _ => query.Where(contract => contract.Status == status)
      };
    }

    if (request.PropertyId.HasValue && request.PropertyId.Value != Guid.Empty)
    {
      var propertyId = new EntityId(request.PropertyId.Value);
      query = query.Where(contract => contract.PropertyId == propertyId);
    }

    if (request.ResidentId.HasValue && request.ResidentId.Value != Guid.Empty)
    {
      var residentId = new EntityId(request.ResidentId.Value);
      query = query.Where(contract => contract.Residents.Any(resident => resident.ResidentId == residentId));
    }

    if (request.StartsFrom.HasValue)
    {
      query = query.Where(contract => contract.StartDate >= request.StartsFrom.Value);
    }

    if (request.StartsTo.HasValue)
    {
      query = query.Where(contract => contract.StartDate <= request.StartsTo.Value);
    }

    if (request.EndsFrom.HasValue)
    {
      query = query.Where(contract => contract.EndDate != null && contract.EndDate >= request.EndsFrom.Value);
    }

    if (request.EndsTo.HasValue)
    {
      query = query.Where(contract => contract.EndDate != null && contract.EndDate <= request.EndsTo.Value);
    }

    if (request.EndingSoonOnly)
    {
      query = query.Where(contract =>
        contract.Status == ContractStatus.Active &&
        contract.EndDate != null &&
        contract.EndDate >= today &&
        contract.EndDate <= today.AddDays(LeaseContract.DefaultEndingSoonDays));
    }

    return query;
  }

  private static IQueryable<LeaseContract> ApplySort(IQueryable<LeaseContract> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return ContractCode.NormalizeCode(string.IsNullOrWhiteSpace(key) ? "start-date" : key!) switch
    {
      "status" => descending
        ? query.OrderByDescending(contract => contract.Status).ThenBy(contract => contract.StartDate)
        : query.OrderBy(contract => contract.Status).ThenBy(contract => contract.StartDate),
      "end-date" => descending
        ? query.OrderByDescending(contract => contract.EndDate).ThenBy(contract => contract.StartDate)
        : query.OrderBy(contract => contract.EndDate).ThenBy(contract => contract.StartDate),
      "rent" or "monthly-rent" => descending
        ? query.OrderByDescending(contract => contract.MonthlyRent.Amount).ThenBy(contract => contract.StartDate)
        : query.OrderBy(contract => contract.MonthlyRent.Amount).ThenBy(contract => contract.StartDate),
      "created-at" => descending
        ? query.OrderByDescending(contract => contract.CreatedAt).ThenBy(contract => contract.Id.Value)
        : query.OrderBy(contract => contract.CreatedAt).ThenBy(contract => contract.Id.Value),
      _ => descending
        ? query.OrderByDescending(contract => contract.StartDate).ThenBy(contract => contract.Id.Value)
        : query.OrderBy(contract => contract.StartDate).ThenBy(contract => contract.Id.Value)
    };
  }

  private async Task<ContractSnapshot> BuildSnapshotAsync(
    LeaseContract contract,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var property = await GetPropertySnapshotAsync(contract.PropertyId, organizationId, cancellationToken)
      .ConfigureAwait(false) ??
      new ContractPropertySnapshot(contract.PropertyId, contract.PropertyId.Value.ToString("D"), null);
    var residentIds = contract.Residents.Select(resident => resident.ResidentId).ToArray();
    var residents = await GetResidentSnapshotsAsync(
        residentIds,
        contract.PrimaryResidentId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

    return new ContractSnapshot(contract, property, residents);
  }
}
