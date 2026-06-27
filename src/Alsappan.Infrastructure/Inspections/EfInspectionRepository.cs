using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Inspections;
using Alsappan.Application.Inspections.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Inspections;

public sealed class EfInspectionRepository : IInspectionRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfInspectionRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<InspectionSnapshot>> ListAsync(
    InspectionListRequestDto request,
    OrganizationId organizationId,
    DateTimeOffset now,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var listFilter = new ListFilterDto(
      request.Page,
      request.PageSize,
      request.Search,
      request.Sort,
      request.IncludeArchived);
    var query = BuildListQuery(request, organizationId, now);
    var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var rows = await ApplySort(query, request.Sort)
      .Skip(listFilter.Offset)
      .Take(listFilter.PageSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var snapshots = new List<InspectionSnapshot>(rows.Count);

    foreach (var inspection in rows)
    {
      var snapshot = await BuildSnapshotAsync(inspection, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (snapshot is not null)
      {
        snapshots.Add(snapshot);
      }
    }

    return new PagedResultDto<InspectionSnapshot>(snapshots, listFilter.Page, listFilter.PageSize, total);
  }

  public Task<Inspection?> FindAsync(
    EntityId inspectionId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = BaseQuery()
      .Where(inspection => inspection.Id == inspectionId && inspection.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(inspection =>
        inspection.DeletedAt == null && inspection.Status != InspectionStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<InspectionSnapshot?> FindSnapshotAsync(
    EntityId inspectionId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var inspection = await FindAsync(inspectionId, organizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    return inspection is null
      ? null
      : await BuildSnapshotAsync(inspection, organizationId, cancellationToken).ConfigureAwait(false);
  }

  public async Task<InspectionPropertySnapshot?> GetPropertySnapshotAsync(
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
      : new InspectionPropertySnapshot(
        property.Id,
        property.Name,
        $"{property.StreetLine}, {property.Number} - {property.Neighborhood}, {property.City}/{property.StateCode}");
  }

  public async Task<InspectionContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var contract = await dbContext.Contracts.IgnoreQueryFilters()
      .Include(candidate => candidate.Residents)
      .AsNoTracking()
      .Where(candidate => candidate.Id == contractId && candidate.OrganizationId == organizationId)
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

    return new InspectionContractSnapshot(
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      residentIds,
      displayName,
      propertyName,
      residentName,
      contract.DeletedAt is not null || contract.Status == ContractStatus.Archived);
  }

  public async Task<InspectionResidentSnapshot?> GetResidentSnapshotAsync(
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
      : new InspectionResidentSnapshot(resident.Id, resident.FullName);
  }

  public async Task<InspectionUserSnapshot?> GetAssigneeSnapshotAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var hasMembership = await dbContext.IdentityMemberships.IgnoreQueryFilters()
      .AnyAsync(membership => membership.OrganizationId == organizationId &&
        membership.UserId == userId &&
        membership.DeletedAt == null &&
        membership.Status == IdentityMembershipStatus.Active,
        cancellationToken)
      .ConfigureAwait(false);
    if (!hasMembership)
    {
      return null;
    }

    var user = await dbContext.IdentityUsers.IgnoreQueryFilters()
      .Where(candidate => candidate.Id == userId &&
        candidate.AccountType == UserAccountType.Admin &&
        candidate.DeletedAt == null &&
        candidate.Status != UserStatus.Archived)
      .Select(candidate => new { candidate.Id, candidate.DisplayName, candidate.Email })
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    if (user is null)
    {
      return null;
    }

    var name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName;
    return new InspectionUserSnapshot(user.Id, name, user.Email);
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

  public async Task AddAsync(Inspection inspection, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(inspection);

    await SyncSharedDocumentLinksAsync(inspection, cancellationToken).ConfigureAwait(false);
    dbContext.Inspections.Add(inspection);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(Inspection inspection, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(inspection);

    await SyncSharedDocumentLinksAsync(inspection, cancellationToken).ConfigureAwait(false);
    dbContext.Inspections.Update(inspection);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private async Task SyncSharedDocumentLinksAsync(Inspection inspection, CancellationToken cancellationToken)
  {
    var documentIds = inspection.DocumentLinks
      .Select(link => link.DocumentId)
      .Concat(inspection.SignatureSlots
        .Where(slot => slot.SignatureDocumentId.HasValue)
        .Select(slot => slot.SignatureDocumentId!.Value))
      .Distinct()
      .ToArray();
    if (documentIds.Length == 0)
    {
      return;
    }

    var activeDocumentIds = inspection.DocumentLinks
      .Where(link => !link.IsDeleted)
      .Select(link => link.DocumentId)
      .Concat(inspection.SignatureSlots
        .Where(slot => !slot.IsDeleted && slot.SignatureDocumentId.HasValue)
        .Select(slot => slot.SignatureDocumentId!.Value))
      .ToHashSet();
    var documents = await dbContext.Documents.IgnoreQueryFilters()
      .Include(document => document.Links)
      .Where(document => document.OrganizationId == inspection.OrganizationId &&
        documentIds.Contains(document.Id))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var changedAt = inspection.UpdatedAt ?? inspection.CreatedAt;
    var changedByUserId = inspection.UpdatedByUserId ?? inspection.CreatedByUserId;

    foreach (var document in documents)
    {
      if (activeDocumentIds.Contains(document.Id) &&
        document.DeletedAt is null &&
        document.Status != DocumentStatus.Archived)
      {
        document.SetEntityLink("inspection", inspection.Id, inspection.Title, changedAt, changedByUserId);
      }
      else
      {
        document.RemoveEntityLink("inspection", inspection.Id, changedAt, changedByUserId);
      }
    }
  }

  private IQueryable<Inspection> BuildListQuery(
    InspectionListRequestDto request,
    OrganizationId organizationId,
    DateTimeOffset now)
  {
    var query = BaseQuery()
      .Where(inspection => inspection.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(inspection =>
        inspection.DeletedAt == null && inspection.Status != InspectionStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = InspectionCode.NormalizeSearchText(request.Search);
      query = query.Where(inspection => inspection.SearchText.Contains(search));
    }

    if (InspectionCatalog.TryParseType(request.Type, out var type))
    {
      query = query.Where(inspection => inspection.Type == type);
    }

    if (InspectionCatalog.TryParseStatus(request.Status, out var status))
    {
      query = status == InspectionStatus.Archived
        ? query.Where(inspection => inspection.DeletedAt != null || inspection.Status == InspectionStatus.Archived)
        : query.Where(inspection => inspection.DeletedAt == null && inspection.Status == status);
    }

    if (request.PropertyId.HasValue && request.PropertyId.Value != Guid.Empty)
    {
      var propertyId = new EntityId(request.PropertyId.Value);
      query = query.Where(inspection => inspection.PropertyId == propertyId);
    }

    if (request.ContractId.HasValue && request.ContractId.Value != Guid.Empty)
    {
      var contractId = new EntityId(request.ContractId.Value);
      query = query.Where(inspection => inspection.ContractId == contractId);
    }

    if (request.ResidentId.HasValue && request.ResidentId.Value != Guid.Empty)
    {
      var residentId = new EntityId(request.ResidentId.Value);
      query = query.Where(inspection => inspection.ResidentId == residentId);
    }

    if (request.AssignedUserId.HasValue && request.AssignedUserId.Value != Guid.Empty)
    {
      var assignedUserId = new UserId(request.AssignedUserId.Value);
      query = query.Where(inspection => inspection.AssignedUserId == assignedUserId);
    }

    if (request.ScheduledFrom.HasValue)
    {
      query = query.Where(inspection => inspection.ScheduledAt >= request.ScheduledFrom.Value);
    }

    if (request.ScheduledTo.HasValue)
    {
      query = query.Where(inspection => inspection.ScheduledAt <= request.ScheduledTo.Value);
    }

    if (request.PendingOnly)
    {
      var pendingUntil = now.AddDays(7);
      query = query.Where(inspection =>
        inspection.DeletedAt == null &&
        (inspection.Status == InspectionStatus.Scheduled || inspection.Status == InspectionStatus.InProgress) &&
        inspection.ScheduledAt <= pendingUntil);
    }

    return query;
  }

  private static IQueryable<Inspection> ApplySort(IQueryable<Inspection> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return InspectionCode.NormalizeCode(string.IsNullOrWhiteSpace(key) ? "scheduled-at" : key!) switch
    {
      "status" => descending
        ? query.OrderByDescending(inspection => inspection.Status).ThenBy(inspection => inspection.ScheduledAt)
        : query.OrderBy(inspection => inspection.Status).ThenBy(inspection => inspection.ScheduledAt),
      "type" => descending
        ? query.OrderByDescending(inspection => inspection.Type).ThenBy(inspection => inspection.ScheduledAt)
        : query.OrderBy(inspection => inspection.Type).ThenBy(inspection => inspection.ScheduledAt),
      "title" => descending
        ? query.OrderByDescending(inspection => inspection.Title).ThenBy(inspection => inspection.ScheduledAt)
        : query.OrderBy(inspection => inspection.Title).ThenBy(inspection => inspection.ScheduledAt),
      "created-at" => descending
        ? query.OrderByDescending(inspection => inspection.CreatedAt).ThenBy(inspection => inspection.Id.Value)
        : query.OrderBy(inspection => inspection.CreatedAt).ThenBy(inspection => inspection.Id.Value),
      _ => descending
        ? query.OrderByDescending(inspection => inspection.ScheduledAt).ThenBy(inspection => inspection.Id.Value)
        : query.OrderBy(inspection => inspection.ScheduledAt).ThenBy(inspection => inspection.Id.Value)
    };
  }

  private async Task<InspectionSnapshot?> BuildSnapshotAsync(
    Inspection inspection,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var property = await GetPropertySnapshotAsync(inspection.PropertyId, organizationId, cancellationToken)
      .ConfigureAwait(false) ??
      new InspectionPropertySnapshot(inspection.PropertyId, inspection.PropertyId.Value.ToString("D"), null);
    var contract = inspection.ContractId.HasValue
      ? await GetContractSnapshotAsync(inspection.ContractId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var resident = inspection.ResidentId.HasValue
      ? await GetResidentSnapshotAsync(inspection.ResidentId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var assignee = await GetAssigneeSnapshotAsync(inspection.AssignedUserId, organizationId, cancellationToken)
      .ConfigureAwait(false) ??
      new InspectionUserSnapshot(
        inspection.AssignedUserId,
        inspection.AssignedUserName ?? inspection.AssignedUserId.Value.ToString("D"),
        null);
    var documents = inspection.DocumentLinks
      .Where(link => link.DeletedAt is null)
      .OrderBy(link => link.Kind)
      .ThenBy(link => link.CreatedAt)
      .Select(link => new InspectionDocumentSnapshot(link.DocumentId, link.ChecklistItemId, link.Kind, link.Label))
      .ToArray();

    return new InspectionSnapshot(inspection, property, contract, resident, assignee, documents);
  }

  private IQueryable<Inspection> BaseQuery() =>
    dbContext.Inspections.IgnoreQueryFilters()
      .Include(inspection => inspection.ChecklistItems)
      .Include(inspection => inspection.DocumentLinks)
      .Include(inspection => inspection.SignatureSlots);
}
