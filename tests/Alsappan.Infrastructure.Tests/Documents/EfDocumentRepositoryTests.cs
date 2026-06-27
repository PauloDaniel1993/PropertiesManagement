using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Documents;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Documents;
using Alsappan.Infrastructure.Documents;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Documents;

public sealed class EfDocumentRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByOrganizationSearchCategoryLinkAndArchivedState()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var contractId = EntityId.New();

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var visible = CreateDocument(organizationA, "Contrato assinado", DocumentCategory.Contract, contractId);
      var otherTenant = CreateDocument(organizationB, "Contrato assinado", DocumentCategory.Contract, contractId);
      var archived = CreateDocument(organizationA, "Contrato arquivado", DocumentCategory.Contract, EntityId.New());
      archived.Archive(DateTimeOffset.UtcNow.AddMinutes(1), null);

      setup.Documents.AddRange(visible, otherTenant, archived);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfDocumentRepository(context);

    var page = await repository.ListAsync(
      new DocumentListRequestDto(
        Search: "contrato",
        Category: "contract",
        LinkedEntityType: "contract",
        LinkedEntityId: contractId.Value),
      organizationA);

    Assert.Single(page.Items);
    Assert.Equal("Contrato assinado", page.Items[0].Document.Title);
    Assert.Single(page.Items[0].Document.Versions);
    Assert.Single(page.Items[0].Document.Links);

    var includingArchived = await repository.ListAsync(
      new DocumentListRequestDto(Search: "contrato", IncludeArchived: true),
      organizationA);

    Assert.Equal(2, includingArchived.TotalItems);
  }

  [Fact]
  public async Task FindAsyncCanIncludeArchivedDocumentsWhenRequested()
  {
    var organizationId = OrganizationId.New();
    var document = CreateDocument(organizationId, "Contrato arquivado", DocumentCategory.Contract, EntityId.New());
    document.Archive(DateTimeOffset.UtcNow.AddMinutes(1), null);

    await using var context = CreateContext(organizationId, Guid.NewGuid().ToString("N"));
    context.Documents.Add(document);
    await context.SaveChangesAsync();
    var repository = new EfDocumentRepository(context);

    Assert.Null(await repository.FindAsync(document.Id, organizationId));
    Assert.NotNull(await repository.FindAsync(document.Id, organizationId, includeArchived: true));
  }

  private static AlsappanDbContext CreateContext(OrganizationId organizationId, string databaseName)
  {
    var options = new DbContextOptionsBuilder<AlsappanDbContext>()
      .UseInMemoryDatabase(databaseName)
      .ReplaceService<IModelCacheKeyFactory, AlsappanModelCacheKeyFactory>()
      .Options;

    return new AlsappanDbContext(
      options,
      new StaticActiveOrganizationContext(organizationId),
      new DatabaseOptions());
  }

  private static DocumentRecord CreateDocument(
    OrganizationId organizationId,
    string title,
    DocumentCategory category,
    EntityId linkedEntityId) =>
    DocumentRecord.Create(
      EntityId.New(),
      organizationId,
      category,
      title,
      "Documento digitalizado",
      $"{DocumentCode.NormalizeCode(title)}.pdf",
      "application/pdf",
      128,
      $"documents/{Guid.NewGuid():N}.pdf",
      "Versao inicial",
      [new DocumentLinkDraft("contract", linkedEntityId, "Contrato 1")],
      DateTimeOffset.UtcNow);
}
