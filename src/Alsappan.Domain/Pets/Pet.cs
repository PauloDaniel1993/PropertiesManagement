using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Pets;

public sealed class Pet : TenantScopedEntity<EntityId>
{
  private readonly List<PetDocumentLink> documentLinks = [];

  private Pet()
  {
  }

  private Pet(
    EntityId id,
    OrganizationId organizationId,
    EntityId residentId,
    EntityId? propertyId,
    EntityId? contractId,
    string name,
    PetSpecies species,
    string? breed,
    PetAuthorizationStatus authorizationStatus,
    string? authorizationNotes,
    string? notes,
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ApplyDetails(
      residentId,
      propertyId,
      contractId,
      name,
      species,
      breed,
      authorizationStatus,
      authorizationNotes,
      notes,
      residentSearchText,
      propertySearchText,
      contractSearchText);
  }

  public EntityId ResidentId { get; private set; }

  public EntityId? PropertyId { get; private set; }

  public EntityId? ContractId { get; private set; }

  public string Name { get; private set; } = string.Empty;

  public PetSpecies Species { get; private set; }

  public string? Breed { get; private set; }

  public PetAuthorizationStatus AuthorizationStatus { get; private set; }

  public string? AuthorizationNotes { get; private set; }

  public string? Notes { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public IReadOnlyCollection<PetDocumentLink> DocumentLinks => documentLinks.AsReadOnly();

  public static Pet Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId residentId,
    EntityId? propertyId,
    EntityId? contractId,
    string name,
    PetSpecies species,
    string? breed,
    PetAuthorizationStatus authorizationStatus,
    string? authorizationNotes,
    string? notes,
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      residentId,
      propertyId,
      contractId,
      name,
      species,
      breed,
      authorizationStatus,
      authorizationNotes,
      notes,
      residentSearchText,
      propertySearchText,
      contractSearchText,
      createdAt,
      createdByUserId);

  public void Update(
    EntityId residentId,
    EntityId? propertyId,
    EntityId? contractId,
    string name,
    PetSpecies species,
    string? breed,
    PetAuthorizationStatus authorizationStatus,
    string? authorizationNotes,
    string? notes,
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    ApplyDetails(
      residentId,
      propertyId,
      contractId,
      name,
      species,
      breed,
      authorizationStatus,
      authorizationNotes,
      notes,
      residentSearchText,
      propertySearchText,
      contractSearchText);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Authorize(string? notes, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    EnsureCanMutate();
    AuthorizationStatus = PetAuthorizationStatus.Authorized;
    AuthorizationNotes = PetCode.Optional(notes, 1000, nameof(notes)) ?? AuthorizationNotes;
    SearchText = BuildSearchText();
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Deny(string? notes, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    EnsureCanMutate();
    AuthorizationStatus = PetAuthorizationStatus.Denied;
    AuthorizationNotes = PetCode.Optional(notes, 1000, nameof(notes)) ?? AuthorizationNotes;
    SearchText = BuildSearchText();
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void LinkDocument(
    EntityId documentId,
    PetDocumentKind kind,
    string? label,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
  {
    EnsureCanMutate();
    if (documentLinks.Any(link => link.DocumentId == documentId && link.Kind == kind && !link.IsDeleted))
    {
      return;
    }

    documentLinks.Add(PetDocumentLink.Create(
      EntityId.New(),
      OrganizationId,
      Id,
      documentId,
      kind,
      label,
      createdAt,
      createdByUserId));
    MarkUpdated(createdAt, createdByUserId);
  }

  public void SetDocumentLink(
    EntityId? documentId,
    PetDocumentKind kind,
    string? label,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    var activeLinks = documentLinks
      .Where(link => link.Kind == kind && !link.IsDeleted)
      .ToArray();
    var activeLink = documentId.HasValue
      ? activeLinks.FirstOrDefault(link => link.DocumentId == documentId.Value)
      : null;
    var changed = false;

    foreach (var link in activeLinks)
    {
      if (activeLink is not null && link.Id == activeLink.Id)
      {
        continue;
      }

      link.Remove(updatedAt, updatedByUserId);
      changed = true;
    }

    if (documentId.HasValue && activeLink is null)
    {
      documentLinks.Add(PetDocumentLink.Create(
        EntityId.New(),
        OrganizationId,
        Id,
        documentId.Value,
        kind,
        label,
        updatedAt,
        updatedByUserId));
      changed = true;
    }

    if (changed)
    {
      MarkUpdated(updatedAt, updatedByUserId);
    }
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    AuthorizationStatus = PetAuthorizationStatus.Archived;
    SearchText = BuildSearchText();
    MarkDeleted(deletedAt, deletedByUserId);
  }

  public void Restore(DateTimeOffset restoredAt, UserId? restoredByUserId)
  {
    if (!IsDeleted)
    {
      return;
    }

    DeletedAt = null;
    DeletedByUserId = null;
    AuthorizationStatus = PetAuthorizationStatus.Pending;
    SearchText = BuildSearchText();
    MarkUpdated(restoredAt, restoredByUserId);
  }

  public PetAuthorizationStatus EffectiveStatus =>
    IsDeleted ? PetAuthorizationStatus.Archived : AuthorizationStatus;

  private void ApplyDetails(
    EntityId residentId,
    EntityId? propertyId,
    EntityId? contractId,
    string name,
    PetSpecies species,
    string? breed,
    PetAuthorizationStatus authorizationStatus,
    string? authorizationNotes,
    string? notes,
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText)
  {
    if (residentId.Value == Guid.Empty)
    {
      throw new ArgumentException("Resident owner is required.", nameof(residentId));
    }

    ResidentId = residentId;
    PropertyId = propertyId;
    ContractId = contractId;
    Name = PetCode.Required(name, 160, nameof(name));
    Species = species;
    Breed = PetCode.Optional(breed, 120, nameof(breed));
    AuthorizationStatus = RequireMutableStatus(authorizationStatus);
    AuthorizationNotes = PetCode.Optional(authorizationNotes, 1000, nameof(authorizationNotes));
    Notes = PetCode.Optional(notes, 2000, nameof(notes));
    SearchText = PetCode.NormalizeSearchText(
      Id.Value.ToString("D"),
      ResidentId.Value.ToString("D"),
      PropertyId?.Value.ToString("D"),
      ContractId?.Value.ToString("D"),
      Name,
      Species.ToString(),
      Breed,
      AuthorizationStatus.ToString(),
      AuthorizationNotes,
      Notes,
      residentSearchText,
      propertySearchText,
      contractSearchText);
  }

  private string BuildSearchText() =>
    PetCode.NormalizeSearchText(
      Id.Value.ToString("D"),
      ResidentId.Value.ToString("D"),
      PropertyId?.Value.ToString("D"),
      ContractId?.Value.ToString("D"),
      Name,
      Species.ToString(),
      Breed,
      AuthorizationStatus.ToString(),
      AuthorizationNotes,
      Notes);

  private void EnsureCanMutate()
  {
    if (IsDeleted || AuthorizationStatus == PetAuthorizationStatus.Archived)
    {
      throw new InvalidOperationException("Archived pets cannot be changed.");
    }
  }

  private static PetAuthorizationStatus RequireMutableStatus(PetAuthorizationStatus status) =>
    status == PetAuthorizationStatus.Archived
      ? throw new ArgumentException("Pet authorization status is not valid for this operation.", nameof(status))
      : status;
}
