using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Residents;
using Alsappan.Application.Residents.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Residents;

public sealed class EfResidentRepository : IResidentRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfResidentRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public Task<Resident?> FindAsync(
    EntityId residentId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Residents.IgnoreQueryFilters()
      .Where(resident => resident.Id == residentId && resident.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(resident => resident.DeletedAt == null && resident.Status != ResidentStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<PagedResultDto<Resident>> ListAsync(
    ResidentListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var listFilter = new ListFilterDto(
      request.Page,
      request.PageSize,
      request.Search,
      request.Sort,
      request.IncludeArchived);
    var query = BuildListQuery(request, organizationId);
    var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var rows = await ApplySort(query, request.Sort)
      .Skip(listFilter.Offset)
      .Take(listFilter.PageSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return new PagedResultDto<Resident>(rows, listFilter.Page, listFilter.PageSize, total);
  }

  public async Task<IReadOnlyList<Resident>> FindPotentialDuplicatesAsync(
    ResidentDuplicateWarningRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var normalizedEmail = ResidentCode.NormalizeEmail(request.Email);
    var normalizedPhone = ResidentCode.NormalizePhone(request.Phone);
    var normalizedDocument = ResidentCode.NormalizeIdentifier(request.DocumentIdentifier);
    var query = dbContext.Residents.IgnoreQueryFilters()
      .Where(resident => resident.OrganizationId == organizationId);

    if (request.IgnoreResidentId.HasValue)
    {
      var ignoredId = new EntityId(request.IgnoreResidentId.Value);
      query = query.Where(resident => resident.Id != ignoredId);
    }

    query = query.Where(resident =>
      normalizedEmail != null && resident.NormalizedEmail == normalizedEmail ||
      normalizedPhone != null && resident.NormalizedPhone == normalizedPhone ||
      normalizedDocument != null && resident.NormalizedDocumentIdentifier == normalizedDocument);

    return await query
      .OrderBy(resident => resident.FullName)
      .Take(10)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task AddAsync(Resident resident, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(resident);

    dbContext.Residents.Add(resident);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(Resident resident, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(resident);

    dbContext.Residents.Update(resident);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<Resident> BuildListQuery(
    ResidentListRequestDto request,
    OrganizationId organizationId)
  {
    var query = dbContext.Residents.IgnoreQueryFilters()
      .Where(resident => resident.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(resident => resident.DeletedAt == null && resident.Status != ResidentStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = ResidentCode.NormalizeSearchText(request.Search);
      query = query.Where(resident => resident.SearchText.Contains(search));
    }

    if (ResidentCatalog.TryParseStatus(request.Status, out var status))
    {
      query = query.Where(resident => resident.Status == status);
    }

    if (ResidentCatalog.TryParsePortalStatus(request.PortalStatus, out var portalStatus))
    {
      query = query.Where(resident => resident.PortalStatus == portalStatus);
    }

    if (request.HasPortalAccess.HasValue)
    {
      query = request.HasPortalAccess.Value
        ? query.Where(resident => resident.LinkedUserId != null && resident.PortalStatus == ResidentPortalStatus.Active)
        : query.Where(resident => resident.LinkedUserId == null || resident.PortalStatus != ResidentPortalStatus.Active);
    }

    return query;
  }

  private static IQueryable<Resident> ApplySort(IQueryable<Resident> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return ResidentCode.NormalizeCode(string.IsNullOrWhiteSpace(key) ? "name" : key!) switch
    {
      "status" => descending
        ? query.OrderByDescending(resident => resident.Status).ThenBy(resident => resident.FullName)
        : query.OrderBy(resident => resident.Status).ThenBy(resident => resident.FullName),
      "portal-status" => descending
        ? query.OrderByDescending(resident => resident.PortalStatus).ThenBy(resident => resident.FullName)
        : query.OrderBy(resident => resident.PortalStatus).ThenBy(resident => resident.FullName),
      "created-at" => descending
        ? query.OrderByDescending(resident => resident.CreatedAt).ThenBy(resident => resident.FullName)
        : query.OrderBy(resident => resident.CreatedAt).ThenBy(resident => resident.FullName),
      _ => descending
        ? query.OrderByDescending(resident => resident.FullName).ThenBy(resident => resident.Id.Value)
        : query.OrderBy(resident => resident.FullName).ThenBy(resident => resident.Id.Value)
    };
  }
}
