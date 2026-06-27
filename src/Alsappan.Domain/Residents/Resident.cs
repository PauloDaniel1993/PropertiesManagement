using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Residents;

public sealed class Resident : TenantScopedEntity<EntityId>
{
  private Resident()
  {
  }

  private Resident(
    EntityId id,
    OrganizationId organizationId,
    string fullName,
    string? preferredName,
    string? email,
    string? phone,
    string? secondaryPhone,
    string? documentType,
    string? documentIdentifier,
    DateOnly? birthDate,
    string? emergencyContactName,
    string? emergencyContactRelationship,
    string? emergencyContactPhone,
    ResidentStatus status,
    ResidentPortalStatus portalStatus,
    ResidentPrivacyOptions privacyFlags,
    string? notes,
    UserId? linkedUserId,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ApplyProfile(
      fullName,
      preferredName,
      email,
      phone,
      secondaryPhone,
      documentType,
      documentIdentifier,
      birthDate,
      emergencyContactName,
      emergencyContactRelationship,
      emergencyContactPhone,
      status,
      portalStatus,
      privacyFlags,
      notes,
      linkedUserId);
  }

  public string FullName { get; private set; } = string.Empty;

  public string? PreferredName { get; private set; }

  public string? Email { get; private set; }

  public string? NormalizedEmail { get; private set; }

  public string? Phone { get; private set; }

  public string? NormalizedPhone { get; private set; }

  public string? SecondaryPhone { get; private set; }

  public string? NormalizedSecondaryPhone { get; private set; }

  public string? DocumentType { get; private set; }

  public string? DocumentIdentifier { get; private set; }

  public string? NormalizedDocumentIdentifier { get; private set; }

  public DateOnly? BirthDate { get; private set; }

  public string? EmergencyContactName { get; private set; }

  public string? EmergencyContactRelationship { get; private set; }

  public string? EmergencyContactPhone { get; private set; }

  public string? NormalizedEmergencyContactPhone { get; private set; }

  public ResidentStatus Status { get; private set; }

  public ResidentPortalStatus PortalStatus { get; private set; }

  public ResidentPrivacyOptions PrivacyFlags { get; private set; }

  public string? Notes { get; private set; }

  public UserId? LinkedUserId { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public bool HasPortalAccess =>
    LinkedUserId.HasValue && PortalStatus == ResidentPortalStatus.Active;

  public static Resident Create(
    EntityId id,
    OrganizationId organizationId,
    string fullName,
    string? preferredName,
    string? email,
    string? phone,
    string? secondaryPhone,
    string? documentType,
    string? documentIdentifier,
    DateOnly? birthDate,
    string? emergencyContactName,
    string? emergencyContactRelationship,
    string? emergencyContactPhone,
    ResidentStatus status,
    ResidentPortalStatus portalStatus,
    ResidentPrivacyOptions privacyFlags,
    string? notes,
    UserId? linkedUserId,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      fullName,
      preferredName,
      email,
      phone,
      secondaryPhone,
      documentType,
      documentIdentifier,
      birthDate,
      emergencyContactName,
      emergencyContactRelationship,
      emergencyContactPhone,
      status,
      portalStatus,
      privacyFlags,
      notes,
      linkedUserId,
      createdAt,
      createdByUserId);

  public void Update(
    string fullName,
    string? preferredName,
    string? email,
    string? phone,
    string? secondaryPhone,
    string? documentType,
    string? documentIdentifier,
    DateOnly? birthDate,
    string? emergencyContactName,
    string? emergencyContactRelationship,
    string? emergencyContactPhone,
    ResidentStatus status,
    ResidentPortalStatus portalStatus,
    ResidentPrivacyOptions privacyFlags,
    string? notes,
    UserId? linkedUserId,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    ApplyProfile(
      fullName,
      preferredName,
      email,
      phone,
      secondaryPhone,
      documentType,
      documentIdentifier,
      birthDate,
      emergencyContactName,
      emergencyContactRelationship,
      emergencyContactPhone,
      status,
      portalStatus,
      privacyFlags,
      notes,
      linkedUserId);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void LinkUser(UserId userId, ResidentPortalStatus portalStatus, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    LinkedUserId = userId;
    PortalStatus = RequirePortalStatus(portalStatus);
    SearchText = BuildSearchText();
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void UnlinkUser(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    LinkedUserId = null;
    PortalStatus = ResidentPortalStatus.NotInvited;
    SearchText = BuildSearchText();
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = ResidentStatus.Archived;
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
    Status = ResidentStatus.Inactive;
    MarkUpdated(restoredAt, restoredByUserId);
  }

  private void ApplyProfile(
    string fullName,
    string? preferredName,
    string? email,
    string? phone,
    string? secondaryPhone,
    string? documentType,
    string? documentIdentifier,
    DateOnly? birthDate,
    string? emergencyContactName,
    string? emergencyContactRelationship,
    string? emergencyContactPhone,
    ResidentStatus status,
    ResidentPortalStatus portalStatus,
    ResidentPrivacyOptions privacyFlags,
    string? notes,
    UserId? linkedUserId)
  {
    FullName = ResidentCode.Required(fullName, nameof(fullName), 180);
    PreferredName = ResidentCode.Optional(preferredName, 120, nameof(preferredName));
    Email = ResidentCode.Optional(email, 320, nameof(email));
    NormalizedEmail = ResidentCode.NormalizeEmail(Email);
    Phone = ResidentCode.Optional(phone, 40, nameof(phone));
    NormalizedPhone = ResidentCode.NormalizePhone(Phone);
    SecondaryPhone = ResidentCode.Optional(secondaryPhone, 40, nameof(secondaryPhone));
    NormalizedSecondaryPhone = ResidentCode.NormalizePhone(SecondaryPhone);
    DocumentType = ResidentCode.Optional(documentType, 40, nameof(documentType));
    DocumentIdentifier = ResidentCode.Optional(documentIdentifier, 80, nameof(documentIdentifier));
    NormalizedDocumentIdentifier = ResidentCode.NormalizeIdentifier(DocumentIdentifier);
    BirthDate = birthDate;
    EmergencyContactName = ResidentCode.Optional(emergencyContactName, 160, nameof(emergencyContactName));
    EmergencyContactRelationship = ResidentCode.Optional(
      emergencyContactRelationship,
      80,
      nameof(emergencyContactRelationship));
    EmergencyContactPhone = ResidentCode.Optional(emergencyContactPhone, 40, nameof(emergencyContactPhone));
    NormalizedEmergencyContactPhone = ResidentCode.NormalizePhone(EmergencyContactPhone);
    Status = RequireMutableStatus(status);
    PortalStatus = RequirePortalStatus(portalStatus);
    PrivacyFlags = privacyFlags;
    Notes = ResidentCode.Optional(notes, 2000, nameof(notes));
    LinkedUserId = linkedUserId;
    SearchText = BuildSearchText();
  }

  private string BuildSearchText() =>
    ResidentCode.NormalizeSearchText(
      FullName,
      PreferredName,
      Email,
      Phone,
      SecondaryPhone,
      DocumentType,
      DocumentIdentifier,
      EmergencyContactName,
      EmergencyContactPhone,
      Status.ToString(),
      PortalStatus.ToString());

  private static ResidentStatus RequireMutableStatus(ResidentStatus status) =>
    status is ResidentStatus.None or ResidentStatus.Archived
      ? throw new ArgumentException("Resident status is not valid for this operation.", nameof(status))
      : status;

  private static ResidentPortalStatus RequirePortalStatus(ResidentPortalStatus status) =>
    status == ResidentPortalStatus.None
      ? throw new ArgumentException("Resident portal status is required.", nameof(status))
      : status;
}
