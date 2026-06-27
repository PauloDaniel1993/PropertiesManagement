using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Pets;

namespace Alsappan.Domain.Tests.Pets;

public sealed class PetTests
{
  [Fact]
  public void AuthorizeAndDenyUpdateAuthorizationStateAndNotes()
  {
    var pet = CreatePet();
    var now = DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    pet.Authorize("Autorizado pelo contrato", now, null);

    Assert.Equal(PetAuthorizationStatus.Authorized, pet.AuthorizationStatus);
    Assert.Equal("Autorizado pelo contrato", pet.AuthorizationNotes);

    pet.Deny("Vacina pendente", now.AddMinutes(1), null);

    Assert.Equal(PetAuthorizationStatus.Denied, pet.AuthorizationStatus);
    Assert.Equal("Vacina pendente", pet.AuthorizationNotes);
  }

  [Fact]
  public void LinkDocumentIgnoresDuplicateKindForSameDocument()
  {
    var pet = CreatePet();
    var documentId = EntityId.New();
    var now = DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    pet.LinkDocument(documentId, PetDocumentKind.VaccinationRecord, "Carteira", now, null);
    pet.LinkDocument(documentId, PetDocumentKind.VaccinationRecord, "Carteira", now.AddMinutes(1), null);

    var link = Assert.Single(pet.DocumentLinks);
    Assert.Equal(documentId, link.DocumentId);
    Assert.Equal(PetDocumentKind.VaccinationRecord, link.Kind);
  }

  [Fact]
  public void ArchiveAndRestoreKeepsLifecycleConsistent()
  {
    var pet = CreatePet();
    var now = DateTimeOffset.Parse("2026-06-27T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    pet.Archive(now, null);

    Assert.True(pet.IsDeleted);
    Assert.Equal(PetAuthorizationStatus.Archived, pet.EffectiveStatus);

    pet.Restore(now.AddMinutes(1), null);

    Assert.False(pet.IsDeleted);
    Assert.Equal(PetAuthorizationStatus.Pending, pet.AuthorizationStatus);
  }

  [Fact]
  public void UpdateRejectsArchivedStatus()
  {
    var pet = CreatePet();

    Assert.Throws<ArgumentException>(() => pet.Update(
      EntityId.New(),
      null,
      null,
      "Luna",
      PetSpecies.Cat,
      null,
      PetAuthorizationStatus.Archived,
      null,
      null,
      "Maria",
      null,
      null,
      DateTimeOffset.UtcNow,
      null));
  }

  private static Pet CreatePet() =>
    Pet.Create(
      EntityId.New(),
      OrganizationId.New(),
      EntityId.New(),
      EntityId.New(),
      EntityId.New(),
      "Luna",
      PetSpecies.Cat,
      "SRD",
      PetAuthorizationStatus.Pending,
      null,
      "Docil",
      "Maria Souza",
      "Casa Calabria",
      "Contrato Casa Calabria",
      DateTimeOffset.Parse("2026-06-01T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
