using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Occurrences;

namespace Alsappan.Domain.Tests.Occurrences;

public sealed class OccurrenceTests
{
  [Fact]
  public void CreateWithAssigneeInitializesAssignedLifecycleAndSearchText()
  {
    var assignedUserId = UserId.New();
    var occurrence = CreateOccurrence(assignedUserId);

    Assert.Equal(OccurrenceStatus.Assigned, occurrence.Status);
    Assert.Equal(OccurrencePriority.High, occurrence.Priority);
    Assert.Equal(assignedUserId, occurrence.AssignedUserId);
    Assert.Contains("VAZAMENTO", occurrence.SearchText, StringComparison.Ordinal);
    Assert.Single(occurrence.StatusHistory);
    Assert.Single(occurrence.PriorityHistory);
    Assert.Single(occurrence.AssignmentHistory);
  }

  [Fact]
  public void WorkflowCapturesCommentsAttachmentsAndHistory()
  {
    var now = DateTimeOffset.UtcNow;
    var userId = UserId.New();
    var occurrence = CreateOccurrence();
    var documentId = EntityId.New();

    occurrence.Assign(userId, "Ana Operadora", "Direcionar manutencao", now.AddMinutes(1), userId);
    occurrence.ChangePriority(OccurrencePriority.Urgent, "Risco de dano", now.AddMinutes(2), userId);
    occurrence.ChangeStatus(OccurrenceStatus.InProgress, "Equipe acionada", now.AddMinutes(3), userId);
    occurrence.AddComment("Morador confirmou acesso.", true, now.AddMinutes(4), userId);
    occurrence.LinkDocument(documentId, "Foto do vazamento", now.AddMinutes(5), userId);
    occurrence.LinkDocument(documentId, "Foto duplicada", now.AddMinutes(6), userId);

    Assert.Equal(OccurrenceStatus.InProgress, occurrence.Status);
    Assert.Equal(OccurrencePriority.Urgent, occurrence.Priority);
    Assert.Single(occurrence.Comments);
    Assert.Single(occurrence.Attachments);
    Assert.True(occurrence.StatusHistory.Count == 3);
    Assert.True(occurrence.PriorityHistory.Count == 2);
    Assert.Single(occurrence.AssignmentHistory);
  }

  [Fact]
  public void ResolveCancelArchiveAndRestoreUseDedicatedLifecycleRules()
  {
    var occurrence = CreateOccurrence();
    var now = DateTimeOffset.UtcNow;

    occurrence.Resolve("Conserto realizado.", now.AddMinutes(1), null);

    Assert.Equal(OccurrenceStatus.Resolved, occurrence.Status);
    Assert.Equal("Conserto realizado.", occurrence.ResolutionNotes);
    Assert.Throws<InvalidOperationException>(() =>
      occurrence.ChangeStatus(OccurrenceStatus.Waiting, null, now.AddMinutes(2), null));

    var archived = CreateOccurrence();
    archived.Archive(now.AddMinutes(3), null);
    Assert.True(archived.IsDeleted);
    Assert.Equal(OccurrenceStatus.Archived, archived.Status);
    Assert.Throws<InvalidOperationException>(() =>
      archived.AddComment("Comentario tardio", false, now.AddMinutes(4), null));

    archived.Restore(now.AddMinutes(5), null);
    Assert.False(archived.IsDeleted);
    Assert.Equal(OccurrenceStatus.Open, archived.Status);

    var cancelled = CreateOccurrence();
    cancelled.Cancel("Solicitacao retirada.", now.AddMinutes(6), null);
    Assert.Equal(OccurrenceStatus.Cancelled, cancelled.Status);
    Assert.Equal("Solicitacao retirada.", cancelled.CancellationNotes);
  }

  private static Occurrence CreateOccurrence(UserId? assignedUserId = null) =>
    Occurrence.Create(
      EntityId.New(),
      OrganizationId.New(),
      "Vazamento na cozinha",
      "Morador relatou vazamento recorrente.",
      OccurrenceType.Maintenance,
      OccurrencePriority.High,
      EntityId.New(),
      EntityId.New(),
      null,
      assignedUserId,
      new DateOnly(2026, 7, 2),
      "Casa Calabria",
      "Joao da Silva",
      null,
      assignedUserId.HasValue ? "Ana Operadora" : null,
      DateTimeOffset.UtcNow,
      null);
}
