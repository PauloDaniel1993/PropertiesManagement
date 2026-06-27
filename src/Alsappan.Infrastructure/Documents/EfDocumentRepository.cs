using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Documents;
using Alsappan.Application.Documents.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Documents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Documents;

public sealed class EfDocumentRepository : IDocumentRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfDocumentRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<DocumentSnapshot>> ListAsync(
    DocumentListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var listFilter = new ListFilterDto(
      request.Page,
      request.PageSize,
      request.Search,
      sort: null,
      includeArchived: request.IncludeArchived);
    var query = BuildListQuery(request, organizationId);
    var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var rows = await query
      .OrderByDescending(document => document.CurrentUploadedAt)
      .ThenBy(document => document.Title)
      .Skip(listFilter.Offset)
      .Take(listFilter.PageSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return new PagedResultDto<DocumentSnapshot>(
      rows.Select(document => new DocumentSnapshot(document)).ToArray(),
      listFilter.Page,
      listFilter.PageSize,
      total);
  }

  public Task<DocumentRecord?> FindAsync(
    EntityId documentId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = BaseQuery()
      .Where(document => document.Id == documentId && document.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(document => document.DeletedAt == null && document.Status != DocumentStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<DocumentSnapshot?> FindSnapshotAsync(
    EntityId documentId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var document = await FindAsync(documentId, organizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    return document is null ? null : new DocumentSnapshot(document);
  }

  public async Task AddAsync(DocumentRecord document, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(document);

    dbContext.Documents.Add(document);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(DocumentRecord document, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(document);

    dbContext.Documents.Update(document);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<DocumentRecord> BuildListQuery(
    DocumentListRequestDto request,
    OrganizationId organizationId)
  {
    var query = BaseQuery()
      .Where(document => document.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(document => document.DeletedAt == null && document.Status != DocumentStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = DocumentCode.NormalizeSearchText(request.Search);
      query = query.Where(document => document.SearchText.Contains(search));
    }

    if (DocumentCatalog.TryParseCategory(request.Category, out var category))
    {
      query = query.Where(document => document.Category == category);
    }

    if (!string.IsNullOrWhiteSpace(request.LinkedEntityType))
    {
      var entityType = DocumentCode.NormalizeCode(request.LinkedEntityType);
      query = query.Where(document => document.Links.Any(link => link.EntityType == entityType));
    }

    if (request.LinkedEntityId.HasValue && request.LinkedEntityId.Value != Guid.Empty)
    {
      var entityId = new EntityId(request.LinkedEntityId.Value);
      query = query.Where(document => document.Links.Any(link => link.EntityId == entityId));
    }

    if (request.UploadedFrom.HasValue)
    {
      var from = new DateTimeOffset(
        request.UploadedFrom.Value.ToDateTime(TimeOnly.MinValue),
        TimeSpan.Zero);
      query = query.Where(document => document.CurrentUploadedAt >= from);
    }

    if (request.UploadedTo.HasValue)
    {
      var to = new DateTimeOffset(
        request.UploadedTo.Value.ToDateTime(TimeOnly.MaxValue),
        TimeSpan.Zero);
      query = query.Where(document => document.CurrentUploadedAt <= to);
    }

    if (request.UploadedByUserId.HasValue && request.UploadedByUserId.Value != Guid.Empty)
    {
      var uploadedByUserId = new UserId(request.UploadedByUserId.Value);
      query = query.Where(document => document.CurrentUploadedByUserId == uploadedByUserId);
    }

    return query;
  }

  private IQueryable<DocumentRecord> BaseQuery() =>
    dbContext.Documents.IgnoreQueryFilters()
      .Include(document => document.Links)
      .Include(document => document.Versions);
}
