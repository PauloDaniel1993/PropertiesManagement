using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Inspections;

namespace Alsappan.Domain.Tests.Inspections;

public sealed class InspectionTests
{
  [Fact]
  public void CreateDefaultsScheduleMetadataAndSearchText()
  {
    var inspection = CreateInspection(title: null);

    Assert.Equal(InspectionStatus.Scheduled, inspection.Status);
    Assert.Equal("Inspection 2026-06-27", inspection.Title);
    Assert.Contains("CASA CALABRIA", inspection.SearchText, StringComparison.Ordinal);
    Assert.Contains("JOAO DA SILVA", inspection.SearchText, StringComparison.Ordinal);
  }

  [Fact]
  public void ChecklistProgressRequiresRequiredItemsToBeRated()
  {
    var inspection = CreateInspection();

    inspection.AddChecklistItem(
      "Sala",
      "Piso",
      isRequired: true,
      InspectionConditionRating.Pending,
      null,
      0,
      DateTimeOffset.UtcNow,
      null);
    var optional = inspection.AddChecklistItem(
      "Area externa",
      "Jardim",
      isRequired: false,
      InspectionConditionRating.Pending,
      null,
      1,
      DateTimeOffset.UtcNow,
      null);

    Assert.Equal(2, inspection.ActiveChecklistItemCount);
    Assert.Equal(1, inspection.CompletedChecklistItemCount);
    Assert.Equal(50m, inspection.CompletionPercentage);

    var required = inspection.ChecklistItems.First(item => item.IsRequired);
    inspection.UpdateChecklistItem(
      required.Id,
      required.AreaName,
      required.ItemName,
      required.IsRequired,
      InspectionConditionRating.Good,
      "Sem danos",
      required.SortOrder,
      DateTimeOffset.UtcNow,
      null);

    Assert.Equal(2, inspection.CompletedChecklistItemCount);
    Assert.Equal(100m, inspection.CompletionPercentage);
    Assert.True(optional.IsComplete);
  }

  [Fact]
  public void CompleteLocksChecklistAndRequiresProgress()
  {
    var inspection = CreateInspection();
    var item = inspection.AddChecklistItem(
      "Cozinha",
      "Bancada",
      isRequired: true,
      InspectionConditionRating.Pending,
      null,
      0,
      DateTimeOffset.UtcNow,
      null);

    Assert.Throws<InvalidOperationException>(() =>
      inspection.Complete("Pronto", DateTimeOffset.UtcNow.AddMinutes(1), null));

    inspection.UpdateChecklistItem(
      item.Id,
      item.AreaName,
      item.ItemName,
      item.IsRequired,
      InspectionConditionRating.Attention,
      "Trinca pequena",
      item.SortOrder,
      DateTimeOffset.UtcNow.AddMinutes(2),
      null);
    inspection.Complete("Concluida com ressalva", DateTimeOffset.UtcNow.AddMinutes(3), null);

    Assert.Equal(InspectionStatus.Completed, inspection.Status);
    Assert.Equal("Concluida com ressalva", inspection.CompletionNotes);
    Assert.Throws<InvalidOperationException>(() =>
      inspection.AddChecklistItem(
        "Quarto",
        "Parede",
        true,
        InspectionConditionRating.Good,
        null,
        2,
        DateTimeOffset.UtcNow.AddMinutes(4),
        null));
  }

  [Fact]
  public void CompleteRequiresRequiredSignatureSlotsToBeSigned()
  {
    var inspection = CreateInspection();
    inspection.AddChecklistItem(
      "Cozinha",
      "Bancada",
      isRequired: true,
      InspectionConditionRating.Good,
      null,
      0,
      DateTimeOffset.UtcNow,
      null);
    inspection.ReplaceSignatureSlots(
      [new InspectionSignatureSlotDraft("Morador", "Joao da Silva", true)],
      DateTimeOffset.UtcNow.AddMinutes(1),
      null);

    Assert.Throws<InvalidOperationException>(() =>
      inspection.Complete("Pronto", DateTimeOffset.UtcNow.AddMinutes(2), null));

    inspection.SignatureSlots.Single().Sign(
      "Joao da Silva",
      null,
      null,
      DateTimeOffset.UtcNow.AddMinutes(3),
      null);
    inspection.Complete("Pronto", DateTimeOffset.UtcNow.AddMinutes(4), null);

    Assert.Equal(InspectionStatus.Completed, inspection.Status);
  }

  [Fact]
  public void DocumentLinksFollowChecklistDeletion()
  {
    var inspection = CreateInspection();
    var item = inspection.AddChecklistItem(
      "Banheiro",
      "Box",
      isRequired: true,
      InspectionConditionRating.Good,
      null,
      0,
      DateTimeOffset.UtcNow,
      null);
    var documentId = EntityId.New();

    inspection.LinkDocument(
      documentId,
      InspectionDocumentKind.Photo,
      item.Id,
      "Foto do box",
      DateTimeOffset.UtcNow.AddMinutes(1),
      null);
    inspection.RemoveChecklistItem(item.Id, DateTimeOffset.UtcNow.AddMinutes(2), null);

    Assert.True(inspection.ChecklistItems.Single().IsDeleted);
    Assert.True(inspection.DocumentLinks.Single().IsDeleted);
  }

  [Fact]
  public void ArchiveAndRestoreResetLifecycleToScheduled()
  {
    var inspection = CreateInspection();

    inspection.Archive(DateTimeOffset.UtcNow.AddMinutes(1), null);
    Assert.True(inspection.IsDeleted);
    Assert.Equal(InspectionStatus.Archived, inspection.Status);

    inspection.Restore(DateTimeOffset.UtcNow.AddMinutes(2), null);
    Assert.False(inspection.IsDeleted);
    Assert.Equal(InspectionStatus.Scheduled, inspection.Status);
  }

  private static Inspection CreateInspection(string? title = "Vistoria inicial") =>
    Inspection.Create(
      EntityId.New(),
      OrganizationId.New(),
      InspectionType.MoveIn,
      EntityId.New(),
      EntityId.New(),
      EntityId.New(),
      new DateTimeOffset(2026, 6, 27, 14, 0, 0, TimeSpan.Zero),
      UserId.New(),
      "Ana Admin",
      title,
      "Observacao",
      "Casa Calabria",
      "Contrato Casa Calabria",
      "Joao da Silva",
      DateTimeOffset.UtcNow,
      null);
}
