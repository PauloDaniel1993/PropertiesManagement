using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Documents;

namespace Alsappan.Domain.Tests.Documents;

public sealed class DocumentRecordTests
{
  [Fact]
  public void CreateSetsCurrentVersionAndDeduplicatesLinks()
  {
    var organizationId = OrganizationId.New();
    var propertyId = EntityId.New();
    var now = DateTimeOffset.UtcNow;

    var document = DocumentRecord.Create(
      EntityId.New(),
      organizationId,
      DocumentCategory.Contract,
      "Contrato assinado",
      "Documento digitalizado",
      "contrato.pdf",
      "application/pdf",
      128,
      "documents/contrato.pdf",
      "Versao inicial",
      [
        new DocumentLinkDraft("contract", propertyId, "Contrato 1"),
        new DocumentLinkDraft("contract", propertyId, "Contrato 1 duplicado")
      ],
      now);

    Assert.Equal(DocumentStatus.Active, document.Status);
    Assert.Equal(1, document.CurrentVersionNumber);
    Assert.Equal("contrato.pdf", document.CurrentFileName);
    Assert.Single(document.Versions);
    Assert.Single(document.Links);
    Assert.Contains("CONTRATO", document.SearchText, StringComparison.Ordinal);
  }

  [Fact]
  public void AddVersionPreservesHistoryAndUpdatesCurrentFile()
  {
    var document = CreateDocument();

    document.AddVersion(
      "contrato-revisado.pdf",
      "application/pdf",
      256,
      "documents/contrato-revisado.pdf",
      "Revisao assinada",
      DateTimeOffset.UtcNow.AddMinutes(1),
      null);

    Assert.Equal(2, document.CurrentVersionNumber);
    Assert.Equal("contrato-revisado.pdf", document.CurrentFileName);
    Assert.Equal([1, 2], document.Versions.Select(version => version.VersionNumber).Order().ToArray());
  }

  [Fact]
  public void ArchiveBlocksMetadataAndVersionChangesUntilRestore()
  {
    var document = CreateDocument();
    document.Archive(DateTimeOffset.UtcNow.AddMinutes(1), null);

    Assert.Equal(DocumentStatus.Archived, document.Status);
    Assert.True(document.IsDeleted);
    Assert.Throws<InvalidOperationException>(() => document.UpdateMetadata(
      DocumentCategory.General,
      "Novo titulo",
      null,
      [],
      DateTimeOffset.UtcNow.AddMinutes(2),
      null));
    Assert.Throws<InvalidOperationException>(() => document.AddVersion(
      "novo.pdf",
      "application/pdf",
      1,
      "documents/novo.pdf",
      null,
      DateTimeOffset.UtcNow.AddMinutes(2),
      null));

    document.Restore(DateTimeOffset.UtcNow.AddMinutes(3), null);

    Assert.Equal(DocumentStatus.Active, document.Status);
    Assert.False(document.IsDeleted);
  }

  private static DocumentRecord CreateDocument() =>
    DocumentRecord.Create(
      EntityId.New(),
      OrganizationId.New(),
      DocumentCategory.Contract,
      "Contrato assinado",
      "Documento digitalizado",
      "contrato.pdf",
      "application/pdf",
      128,
      "documents/contrato.pdf",
      "Versao inicial",
      [new DocumentLinkDraft("contract", EntityId.New(), "Contrato 1")],
      DateTimeOffset.UtcNow);
}
