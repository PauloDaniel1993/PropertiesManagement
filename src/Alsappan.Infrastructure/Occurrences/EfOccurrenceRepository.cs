using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Occurrences;
using Alsappan.Application.Occurrences.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Occurrences;

public sealed class EfOccurrenceRepository : IOccurrenceRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfOccurrenceRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<OccurrenceSnapshot>> ListAsync(
    OccurrenceListRequestDto request,
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
    var snapshots = new List<OccurrenceSnapshot>(rows.Count);

    foreach (var occurrence in rows)
    {
      snapshots.Add(await BuildSnapshotAsync(occurrence, organizationId, cancellationToken).ConfigureAwait(false));
    }

    return new PagedResultDto<OccurrenceSnapshot>(snapshots, listFilter.Page, listFilter.PageSize, total);
  }

  public Task<Occurrence?> FindAsync(
    EntityId occurrenceId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = BaseQuery()
      .Where(occurrence => occurrence.Id == occurrenceId && occurrence.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(occurrence =>
        occurrence.DeletedAt == null && occurrence.Status != OccurrenceStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<OccurrenceSnapshot?> FindSnapshotAsync(
    EntityId occurrenceId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var occurrence = await FindAsync(occurrenceId, organizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    return occurrence is null
      ? null
      : await BuildSnapshotAsync(occurrence, organizationId, cancellationToken).ConfigureAwait(false);
  }

  public async Task<OccurrenceContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var contract = await dbContext.Contracts.IgnoreQueryFilters()
      .Include(candidate => candidate.Residents)
      .AsNoTracking()
      .Where(candidate => candidate.Id == contractId &&
        candidate.OrganizationId == organizationId &&
        candidate.DeletedAt == null &&
        candidate.Status != ContractStatus.Archived)
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
    var residentIds = contract.Residents
      .Where(contractResident => contractResident.DeletedAt is null)
      .Select(contractResident => contractResident.ResidentId)
      .DefaultIfEmpty(contract.PrimaryResidentId)
      .Distinct()
      .ToArray();

    return new OccurrenceContractSnapshot(
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      residentIds,
      displayName,
      propertyName,
      residentName);
  }

  public async Task<OccurrencePropertySnapshot?> GetPropertySnapshotAsync(
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
      : new OccurrencePropertySnapshot(
        property.Id,
        property.Name,
        $"{property.StreetLine}, {property.Number} - {property.Neighborhood}, {property.City}/{property.StateCode}");
  }

  public async Task<OccurrenceResidentSnapshot?> GetResidentSnapshotAsync(
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
      : new OccurrenceResidentSnapshot(resident.Id, resident.FullName);
  }

  public async Task<OccurrenceUserSnapshot?> GetAssignableUserSnapshotAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default) =>
    await GetUserSnapshotAsync(userId, organizationId, requireActive: true, cancellationToken)
      .ConfigureAwait(false);

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

  public async Task AddAsync(Occurrence occurrence, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(occurrence);

    dbContext.Occurrences.Add(occurrence);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(Occurrence occurrence, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(occurrence);

    dbContext.Occurrences.Update(occurrence);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<Occurrence> BuildListQuery(
    OccurrenceListRequestDto request,
    OrganizationId organizationId)
  {
    var query = BaseQuery()
      .Where(occurrence => occurrence.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(occurrence =>
        occurrence.DeletedAt == null && occurrence.Status != OccurrenceStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = OccurrenceCode.NormalizeSearchText(request.Search);
      query = query.Where(occurrence => occurrence.SearchText.Contains(search));
    }

    if (OccurrenceCatalog.TryParseType(request.Type, out var type))
    {
      query = query.Where(occurrence => occurrence.Type == type);
    }

    if (OccurrenceCatalog.TryParsePriority(request.Priority, out var priority))
    {
      query = query.Where(occurrence => occurrence.Priority == priority);
    }

    if (OccurrenceCatalog.TryParseStatus(request.Status, out var status))
    {
      query = status == OccurrenceStatus.Archived
        ? query.Where(occurrence => occurrence.DeletedAt != null || occurrence.Status == OccurrenceStatus.Archived)
        : query.Where(occurrence => occurrence.DeletedAt == null && occurrence.Status == status);
    }

    if (request.AssignedUserId.HasValue && request.AssignedUserId.Value != Guid.Empty)
    {
      var assignedUserId = new UserId(request.AssignedUserId.Value);
      query = query.Where(occurrence => occurrence.AssignedUserId == assignedUserId);
    }

    if (request.PropertyId.HasValue && request.PropertyId.Value != Guid.Empty)
    {
      var propertyId = new EntityId(request.PropertyId.Value);
      query = query.Where(occurrence => occurrence.PropertyId == propertyId);
    }

    if (request.ResidentId.HasValue && request.ResidentId.Value != Guid.Empty)
    {
      var residentId = new EntityId(request.ResidentId.Value);
      query = query.Where(occurrence => occurrence.ResidentId == residentId);
    }

    if (request.ContractId.HasValue && request.ContractId.Value != Guid.Empty)
    {
      var contractId = new EntityId(request.ContractId.Value);
      query = query.Where(occurrence => occurrence.ContractId == contractId);
    }

    if (request.DateFrom.HasValue)
    {
      var fromInstant = new DateTimeOffset(request.DateFrom.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
      query = query.Where(occurrence =>
        occurrence.DueDate >= request.DateFrom.Value ||
        (!occurrence.DueDate.HasValue && occurrence.CreatedAt >= fromInstant));
    }

    if (request.DateTo.HasValue)
    {
      var toInstant = new DateTimeOffset(
        request.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
        TimeSpan.Zero);
      query = query.Where(occurrence =>
        occurrence.DueDate <= request.DateTo.Value ||
        (!occurrence.DueDate.HasValue && occurrence.CreatedAt < toInstant));
    }

    if (request.UnresolvedOnly)
    {
      query = query.Where(occurrence =>
        occurrence.DeletedAt == null &&
        occurrence.Status != OccurrenceStatus.Resolved &&
        occurrence.Status != OccurrenceStatus.Cancelled &&
        occurrence.Status != OccurrenceStatus.Archived);
    }

    return query;
  }

  private static IQueryable<Occurrence> ApplySort(IQueryable<Occurrence> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return OccurrenceCode.NormalizeCode(string.IsNullOrWhiteSpace(key) ? "urgency" : key!) switch
    {
      "title" => descending
        ? query.OrderByDescending(occurrence => occurrence.Title).ThenBy(occurrence => occurrence.Id.Value)
        : query.OrderBy(occurrence => occurrence.Title).ThenBy(occurrence => occurrence.Id.Value),
      "status" => descending
        ? query.OrderByDescending(occurrence => occurrence.Status).ThenByDescending(occurrence => occurrence.Priority)
        : query.OrderBy(occurrence => occurrence.Status).ThenByDescending(occurrence => occurrence.Priority),
      "priority" or "urgency" => descending
        ? query.OrderBy(occurrence => occurrence.Priority).ThenByDescending(occurrence => occurrence.DueDate)
        : query.OrderByDescending(occurrence => occurrence.Priority).ThenBy(occurrence => occurrence.DueDate),
      "due-date" or "date" => descending
        ? query.OrderByDescending(occurrence => occurrence.DueDate).ThenByDescending(occurrence => occurrence.CreatedAt)
        : query.OrderBy(occurrence => occurrence.DueDate).ThenBy(occurrence => occurrence.CreatedAt),
      "created-at" => descending
        ? query.OrderByDescending(occurrence => occurrence.CreatedAt).ThenBy(occurrence => occurrence.Id.Value)
        : query.OrderBy(occurrence => occurrence.CreatedAt).ThenBy(occurrence => occurrence.Id.Value),
      _ => query
        .OrderByDescending(occurrence => occurrence.Priority)
        .ThenBy(occurrence => occurrence.DueDate)
        .ThenByDescending(occurrence => occurrence.CreatedAt)
    };
  }

  private async Task<OccurrenceSnapshot> BuildSnapshotAsync(
    Occurrence occurrence,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var contract = occurrence.ContractId.HasValue
      ? await GetContractSnapshotAsync(occurrence.ContractId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var propertyId = occurrence.PropertyId ?? contract?.PropertyId;
    var residentId = occurrence.ResidentId ?? contract?.PrimaryResidentId;
    var property = propertyId.HasValue
      ? await GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var resident = residentId.HasValue
      ? await GetResidentSnapshotAsync(residentId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var assignedUser = occurrence.AssignedUserId.HasValue
      ? await GetUserSnapshotAsync(occurrence.AssignedUserId.Value, organizationId, false, cancellationToken).ConfigureAwait(false)
      : null;

    var comments = new List<OccurrenceCommentSnapshot>();
    foreach (var comment in occurrence.Comments.Where(comment => comment.DeletedAt is null).OrderBy(comment => comment.CreatedAt))
    {
      var author = comment.CreatedByUserId.HasValue
        ? await GetUserSnapshotAsync(comment.CreatedByUserId.Value, organizationId, false, cancellationToken).ConfigureAwait(false)
        : null;
      comments.Add(new OccurrenceCommentSnapshot(comment, author));
    }

    var attachments = occurrence.Attachments
      .Where(attachment => attachment.DeletedAt is null)
      .OrderBy(attachment => attachment.CreatedAt)
      .Select(attachment => new OccurrenceDocumentSnapshot(attachment.DocumentId, attachment.Label, attachment.CreatedAt))
      .ToArray();

    var statusHistory = new List<OccurrenceStatusHistorySnapshot>();
    foreach (var history in occurrence.StatusHistory.Where(history => history.DeletedAt is null).OrderBy(history => history.CreatedAt))
    {
      var actor = history.CreatedByUserId.HasValue
        ? await GetUserSnapshotAsync(history.CreatedByUserId.Value, organizationId, false, cancellationToken).ConfigureAwait(false)
        : null;
      statusHistory.Add(new OccurrenceStatusHistorySnapshot(history, actor));
    }

    var priorityHistory = new List<OccurrencePriorityHistorySnapshot>();
    foreach (var history in occurrence.PriorityHistory.Where(history => history.DeletedAt is null).OrderBy(history => history.CreatedAt))
    {
      var actor = history.CreatedByUserId.HasValue
        ? await GetUserSnapshotAsync(history.CreatedByUserId.Value, organizationId, false, cancellationToken).ConfigureAwait(false)
        : null;
      priorityHistory.Add(new OccurrencePriorityHistorySnapshot(history, actor));
    }

    var assignmentHistory = new List<OccurrenceAssignmentHistorySnapshot>();
    foreach (var history in occurrence.AssignmentHistory.Where(history => history.DeletedAt is null).OrderBy(history => history.CreatedAt))
    {
      var previous = history.PreviousAssignedUserId.HasValue
        ? await GetUserSnapshotAsync(history.PreviousAssignedUserId.Value, organizationId, false, cancellationToken).ConfigureAwait(false)
        : null;
      var next = history.NewAssignedUserId.HasValue
        ? await GetUserSnapshotAsync(history.NewAssignedUserId.Value, organizationId, false, cancellationToken).ConfigureAwait(false)
        : null;
      var actor = history.CreatedByUserId.HasValue
        ? await GetUserSnapshotAsync(history.CreatedByUserId.Value, organizationId, false, cancellationToken).ConfigureAwait(false)
        : null;
      assignmentHistory.Add(new OccurrenceAssignmentHistorySnapshot(history, previous, next, actor));
    }

    return new OccurrenceSnapshot(
      occurrence,
      property,
      resident,
      contract,
      assignedUser,
      comments,
      attachments,
      statusHistory,
      priorityHistory,
      assignmentHistory);
  }

  private async Task<OccurrenceUserSnapshot?> GetUserSnapshotAsync(
    UserId userId,
    OrganizationId organizationId,
    bool requireActive,
    CancellationToken cancellationToken)
  {
    var query = dbContext.IdentityMemberships.IgnoreQueryFilters()
      .Join(
        dbContext.IdentityUsers.IgnoreQueryFilters(),
        membership => membership.UserId,
        user => user.Id,
        (membership, user) => new { Membership = membership, User = user })
      .Where(row => row.Membership.OrganizationId == organizationId && row.Membership.UserId == userId);

    if (requireActive)
    {
      query = query.Where(row =>
        row.Membership.DeletedAt == null &&
        row.Membership.Status == IdentityMembershipStatus.Active &&
        row.User.DeletedAt == null &&
        row.User.Status == UserStatus.Active);
    }

    var snapshot = await query
      .Select(row => new
      {
        row.User.Id,
        row.User.DisplayName,
        row.User.Email
      })
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    return snapshot is null
      ? null
      : new OccurrenceUserSnapshot(
        snapshot.Id,
        string.IsNullOrWhiteSpace(snapshot.DisplayName) ? snapshot.Email : snapshot.DisplayName!,
        snapshot.Email);
  }

  private IQueryable<Occurrence> BaseQuery() =>
    dbContext.Occurrences.IgnoreQueryFilters()
      .Include(occurrence => occurrence.Comments)
      .Include(occurrence => occurrence.Attachments)
      .Include(occurrence => occurrence.StatusHistory)
      .Include(occurrence => occurrence.PriorityHistory)
      .Include(occurrence => occurrence.AssignmentHistory);
}
