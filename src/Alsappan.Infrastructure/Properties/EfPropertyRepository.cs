using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Properties;
using Alsappan.Application.Properties.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Properties;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Properties;

public sealed class EfPropertyRepository : IPropertyRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfPropertyRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public Task<RentalProperty?> FindAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Properties.IgnoreQueryFilters()
      .Where(property => property.Id == propertyId && property.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(property => property.DeletedAt == null && property.Status != PropertyStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<PagedResultDto<RentalProperty>> ListAsync(
    PropertyListRequestDto request,
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

    return new PagedResultDto<RentalProperty>(rows, listFilter.Page, listFilter.PageSize, total);
  }

  public async Task AddAsync(RentalProperty rentalProperty, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(rentalProperty);

    dbContext.Properties.Add(rentalProperty);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(RentalProperty rentalProperty, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(rentalProperty);

    dbContext.Properties.Update(rentalProperty);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<RentalProperty> BuildListQuery(
    PropertyListRequestDto request,
    OrganizationId organizationId)
  {
    var query = dbContext.Properties.IgnoreQueryFilters()
      .Where(property => property.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(property => property.DeletedAt == null && property.Status != PropertyStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = PropertyCode.NormalizeSearchText(request.Search);
      query = query.Where(property => property.SearchText.Contains(search));
    }

    if (PropertyCatalog.TryParseStatus(request.Status, out var status))
    {
      query = query.Where(property => property.Status == status);
    }

    if (PropertyCatalog.TryParseType(request.Type, out var type))
    {
      query = query.Where(property => property.Type == type);
    }

    if (request.HasGarage.HasValue)
    {
      query = request.HasGarage.Value
        ? query.Where(property => property.GarageSpaceCount > 0)
        : query.Where(property => property.GarageSpaceCount == 0);
    }

    if (request.MinRent.HasValue)
    {
      query = query.Where(property => property.SuggestedRent.Amount >= request.MinRent.Value);
    }

    if (request.MaxRent.HasValue)
    {
      query = query.Where(property => property.SuggestedRent.Amount <= request.MaxRent.Value);
    }

    return query;
  }

  private static IQueryable<RentalProperty> ApplySort(IQueryable<RentalProperty> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return PropertyCode.NormalizeCode(string.IsNullOrWhiteSpace(key) ? "name" : key!) switch
    {
      "city" => descending
        ? query.OrderByDescending(property => property.Address.City).ThenBy(property => property.Name)
        : query.OrderBy(property => property.Address.City).ThenBy(property => property.Name),
      "status" => descending
        ? query.OrderByDescending(property => property.Status).ThenBy(property => property.Name)
        : query.OrderBy(property => property.Status).ThenBy(property => property.Name),
      "rent" or "suggested-rent" => descending
        ? query.OrderByDescending(property => property.SuggestedRent.Amount).ThenBy(property => property.Name)
        : query.OrderBy(property => property.SuggestedRent.Amount).ThenBy(property => property.Name),
      "created-at" => descending
        ? query.OrderByDescending(property => property.CreatedAt).ThenBy(property => property.Name)
        : query.OrderBy(property => property.CreatedAt).ThenBy(property => property.Name),
      _ => descending
        ? query.OrderByDescending(property => property.Name).ThenBy(property => property.Id.Value)
        : query.OrderBy(property => property.Name).ThenBy(property => property.Id.Value)
    };
  }
}
