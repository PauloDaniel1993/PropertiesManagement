using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Pets;
using Alsappan.Application.Pets.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Pets;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Pets;

public sealed class EfPetRepository : IPetRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfPetRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<PetSnapshot>> ListAsync(
    PetListRequestDto request,
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
    var snapshots = new List<PetSnapshot>(rows.Count);

    foreach (var pet in rows)
    {
      var snapshot = await BuildSnapshotAsync(pet, organizationId, cancellationToken).ConfigureAwait(false);
      if (snapshot is not null)
      {
        snapshots.Add(snapshot);
      }
    }

    return new PagedResultDto<PetSnapshot>(snapshots, listFilter.Page, listFilter.PageSize, total);
  }

  public Task<Pet?> FindAsync(
    EntityId petId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = BaseQuery()
      .Where(pet => pet.Id == petId && pet.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(pet => pet.DeletedAt == null && pet.AuthorizationStatus != PetAuthorizationStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<PetSnapshot?> FindSnapshotAsync(
    EntityId petId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var pet = await FindAsync(petId, organizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    return pet is null ? null : await BuildSnapshotAsync(pet, organizationId, cancellationToken).ConfigureAwait(false);
  }

  public async Task<PetResidentSnapshot?> GetResidentSnapshotAsync(
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
      : new PetResidentSnapshot(resident.Id, resident.FullName);
  }

  public async Task<PetPropertySnapshot?> GetPropertySnapshotAsync(
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
      : new PetPropertySnapshot(
        property.Id,
        property.Name,
        $"{property.StreetLine}, {property.Number} - {property.Neighborhood}, {property.City}/{property.StateCode}");
  }

  public async Task<PetContractSnapshot?> GetContractSnapshotAsync(
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

    return new PetContractSnapshot(
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      contract.Residents.Select(contractResident => contractResident.ResidentId).ToArray(),
      displayName,
      propertyName,
      residentName,
      contract.DeletedAt is null && contract.Status == ContractStatus.Active);
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

  public async Task AddAsync(Pet pet, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(pet);

    await SyncSharedDocumentLinksAsync(pet, cancellationToken).ConfigureAwait(false);
    dbContext.Pets.Add(pet);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(Pet pet, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(pet);

    await SyncSharedDocumentLinksAsync(pet, cancellationToken).ConfigureAwait(false);
    dbContext.Pets.Update(pet);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private async Task SyncSharedDocumentLinksAsync(Pet pet, CancellationToken cancellationToken)
  {
    var documentIds = pet.DocumentLinks
      .Select(link => link.DocumentId)
      .Distinct()
      .ToArray();
    if (documentIds.Length == 0)
    {
      return;
    }

    var activeDocumentIds = pet.DocumentLinks
      .Where(link => !link.IsDeleted)
      .Select(link => link.DocumentId)
      .ToHashSet();
    var documents = await dbContext.Documents.IgnoreQueryFilters()
      .Include(document => document.Links)
      .Where(document => document.OrganizationId == pet.OrganizationId &&
        documentIds.Contains(document.Id))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var changedAt = pet.UpdatedAt ?? pet.CreatedAt;
    var changedByUserId = pet.UpdatedByUserId ?? pet.CreatedByUserId;

    foreach (var document in documents)
    {
      if (activeDocumentIds.Contains(document.Id) &&
        document.DeletedAt is null &&
        document.Status != DocumentStatus.Archived)
      {
        document.SetEntityLink("pet", pet.Id, pet.Name, changedAt, changedByUserId);
      }
      else
      {
        document.RemoveEntityLink("pet", pet.Id, changedAt, changedByUserId);
      }
    }
  }

  private IQueryable<Pet> BuildListQuery(PetListRequestDto request, OrganizationId organizationId)
  {
    var query = BaseQuery()
      .Where(pet => pet.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(pet => pet.DeletedAt == null && pet.AuthorizationStatus != PetAuthorizationStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = PetCode.NormalizeSearchText(request.Search);
      query = query.Where(pet => pet.SearchText.Contains(search));
    }

    if (request.ResidentId.HasValue && request.ResidentId.Value != Guid.Empty)
    {
      var residentId = new EntityId(request.ResidentId.Value);
      query = query.Where(pet => pet.ResidentId == residentId);
    }

    if (request.PropertyId.HasValue && request.PropertyId.Value != Guid.Empty)
    {
      var propertyId = new EntityId(request.PropertyId.Value);
      query = query.Where(pet => pet.PropertyId == propertyId);
    }

    if (request.ContractId.HasValue && request.ContractId.Value != Guid.Empty)
    {
      var contractId = new EntityId(request.ContractId.Value);
      query = query.Where(pet => pet.ContractId == contractId);
    }

    if (PetCatalog.TryParseSpecies(request.Species, out var species))
    {
      query = query.Where(pet => pet.Species == species);
    }

    if (PetCatalog.TryParseAuthorizationStatus(request.AuthorizationStatus, out var authorizationStatus))
    {
      query = authorizationStatus == PetAuthorizationStatus.Archived
        ? query.Where(pet => pet.DeletedAt != null || pet.AuthorizationStatus == PetAuthorizationStatus.Archived)
        : query.Where(pet => pet.DeletedAt == null && pet.AuthorizationStatus == authorizationStatus);
    }

    if (request.ActiveContractOnly)
    {
      var activeContracts = dbContext.Contracts.IgnoreQueryFilters()
        .Where(contract => contract.OrganizationId == organizationId &&
          contract.DeletedAt == null &&
          contract.Status == ContractStatus.Active);
      query = query.Where(pet => pet.ContractId.HasValue &&
        activeContracts.Any(contract => contract.Id == pet.ContractId.Value));
    }

    return query;
  }

  private static IQueryable<Pet> ApplySort(IQueryable<Pet> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return NormalizeSortKey(string.IsNullOrWhiteSpace(key) ? "name" : key!) switch
    {
      "CREATED-AT" => descending
        ? query.OrderByDescending(pet => pet.CreatedAt).ThenBy(pet => pet.Id.Value)
        : query.OrderBy(pet => pet.CreatedAt).ThenBy(pet => pet.Id.Value),
      "SPECIES" => descending
        ? query.OrderByDescending(pet => pet.Species).ThenBy(pet => pet.Name)
        : query.OrderBy(pet => pet.Species).ThenBy(pet => pet.Name),
      "STATUS" or "AUTHORIZATION-STATUS" => descending
        ? query.OrderByDescending(pet => pet.AuthorizationStatus).ThenBy(pet => pet.Name)
        : query.OrderBy(pet => pet.AuthorizationStatus).ThenBy(pet => pet.Name),
      _ => descending
        ? query.OrderByDescending(pet => pet.Name).ThenBy(pet => pet.Id.Value)
        : query.OrderBy(pet => pet.Name).ThenBy(pet => pet.Id.Value)
    };
  }

  private async Task<PetSnapshot?> BuildSnapshotAsync(
    Pet pet,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var resident = await GetResidentSnapshotAsync(pet.ResidentId, organizationId, cancellationToken)
      .ConfigureAwait(false);
    if (resident is null)
    {
      return null;
    }

    var contract = pet.ContractId.HasValue
      ? await GetContractSnapshotAsync(pet.ContractId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var propertyId = pet.PropertyId ?? contract?.PropertyId;
    var property = propertyId.HasValue
      ? await GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var documents = pet.DocumentLinks
      .Where(link => link.DeletedAt is null)
      .OrderBy(link => link.Kind)
      .ThenBy(link => link.CreatedAt)
      .Select(link => new PetDocumentSnapshot(link.DocumentId, link.Kind, link.Label))
      .ToArray();

    return new PetSnapshot(pet, resident, property, contract, documents);
  }

  private IQueryable<Pet> BaseQuery() =>
    dbContext.Pets.IgnoreQueryFilters()
      .Include(pet => pet.DocumentLinks);

  private static string NormalizeSortKey(string value) =>
    value.Trim().ToUpperInvariant();
}
