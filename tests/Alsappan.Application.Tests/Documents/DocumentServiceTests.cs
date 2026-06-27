using System.Text;
using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Files;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Documents;
using Alsappan.Application.Documents.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Documents;

namespace Alsappan.Application.Tests.Documents;

public sealed class DocumentServiceTests
{
  [Fact]
  public async Task UploadAsyncStoresFileAndWritesDocumentSideEffects()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeDocumentRepository();
    var storage = new RecordingFileStorageProvider();
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      storage,
      audit,
      outbox,
      [
        PermissionCodes.Write(PermissionModules.Documents),
        PermissionCodes.Read(PermissionModules.Contracts)
      ]);

    using var content = StreamFor("conteudo do pdf");
    var result = await service.UploadAsync(new DocumentUploadRequestDto(
      "contract",
      "Contrato assinado",
      "Documento digitalizado",
      [new DocumentLinkRequestDto("contract", Guid.NewGuid(), "Contrato 1")],
      "contrato.pdf",
      "application/pdf",
      content.Length,
      content,
      "Versao inicial"));

    Assert.True(result.Succeeded);
    Assert.Single(repository.Documents);
    Assert.Single(storage.StoredFiles);
    Assert.Single(audit.Entries);
    Assert.Single(outbox.Envelopes);
    Assert.Equal("document.uploaded", outbox.Envelopes[0].EventName);
    Assert.Single(result.Value!.Links);
    Assert.DoesNotContain(outbox.Envelopes[0].Data.Keys, key => key.Contains("storage", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public async Task UploadAsyncRejectsUnsupportedFileWithoutSaving()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakeDocumentRepository(),
      new RecordingFileStorageProvider(),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Documents)]);

    using var content = StreamFor("binario");
    var result = await service.UploadAsync(new DocumentUploadRequestDto(
      "contract",
      "Arquivo",
      null,
      [],
      "arquivo.exe",
      "application/octet-stream",
      content.Length,
      content));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("fileName", result.Errors!.Keys);
  }

  [Fact]
  public async Task UploadAsyncRejectsLinksWithoutReadPermission()
  {
    var storage = new RecordingFileStorageProvider();
    var service = CreateService(
      OrganizationId.New(),
      new FakeDocumentRepository(),
      storage,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Documents)]);

    using var content = StreamFor("pdf");
    var result = await service.UploadAsync(new DocumentUploadRequestDto(
      "contract",
      "Contrato assinado",
      null,
      [new DocumentLinkRequestDto("contract", Guid.NewGuid(), "Contrato 1")],
      "contrato.pdf",
      "application/pdf",
      content.Length,
      content));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("links", result.Errors!.Keys);
    Assert.Empty(storage.StoredFiles);
  }

  [Fact]
  public async Task DownloadVersionAsyncReturnsHistoricalFileAndWritesEvent()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeDocumentRepository();
    var storage = new RecordingFileStorageProvider();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      storage,
      new RecordingAuditWriter(),
      outbox,
      [
        PermissionCodes.Write(PermissionModules.Documents),
        PermissionCodes.Read(PermissionModules.Documents)
      ]);

    using var originalContent = StreamFor("original");
    var upload = await service.UploadAsync(new DocumentUploadRequestDto(
      "contract",
      "Contrato assinado",
      null,
      [],
      "contrato.pdf",
      "application/pdf",
      originalContent.Length,
      originalContent));
    using var revisedContent = StreamFor("revisado");
    await service.UploadVersionAsync(upload.Value!.Id, new DocumentVersionUploadRequestDto(
      "contrato-revisado.pdf",
      "application/pdf",
      revisedContent.Length,
      revisedContent,
      "Revisao"));

    var result = await service.DownloadVersionAsync(upload.Value.Id, 1);

    Assert.True(result.Succeeded);
    Assert.Equal("contrato.pdf", result.Value!.FileName);
    using var reader = new StreamReader(result.Value.Content, Encoding.UTF8);
    Assert.Equal("original", await reader.ReadToEndAsync());
    Assert.Contains(outbox.Envelopes, envelope => envelope.EventName == "document.version-downloaded");
  }

  [Fact]
  public async Task UpdateAsyncRejectsStaleConcurrencyToken()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeDocumentRepository();
    var service = CreateService(
      organizationId,
      repository,
      new RecordingFileStorageProvider(),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Documents)]);
    var document = CreateDocument(organizationId);
    repository.Documents.Add(document);

    var result = await service.UpdateAsync(document.Id.Value, new DocumentUpdateRequestDto(
      "contract",
      "Contrato atualizado",
      null,
      [],
      "stale-token"));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Conflict, result.Failure);
    Assert.Contains("concurrencyToken", result.Errors!.Keys);
  }

  [Fact]
  public async Task ListAsyncRequiresReadPermission()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakeDocumentRepository(),
      new RecordingFileStorageProvider(),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Documents)]);

    var result = await service.ListAsync(new DocumentListRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  private static DocumentService CreateService(
    OrganizationId organizationId,
    FakeDocumentRepository repository,
    RecordingFileStorageProvider storage,
    RecordingAuditWriter auditWriter,
    RecordingOutboxWriter outboxWriter,
    IEnumerable<string> permissions) =>
    new(
      repository,
      storage,
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      auditWriter,
      outboxWriter,
      TimeProvider.System);

  private static MemoryStream StreamFor(string value) =>
    new(Encoding.UTF8.GetBytes(value));

  private static DocumentRecord CreateDocument(OrganizationId organizationId) =>
    DocumentRecord.Create(
      EntityId.New(),
      organizationId,
      DocumentCategory.Contract,
      "Contrato assinado",
      null,
      "contrato.pdf",
      "application/pdf",
      128,
      "documents/contrato.pdf",
      null,
      [],
      DateTimeOffset.UtcNow);

  private sealed class FakeDocumentRepository : IDocumentRepository
  {
    public List<DocumentRecord> Documents { get; } = [];

    public Task<PagedResultDto<DocumentSnapshot>> ListAsync(
      DocumentListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = Documents
        .Where(document => document.OrganizationId == organizationId && (request.IncludeArchived || !document.IsDeleted))
        .Select(document => new DocumentSnapshot(document))
        .ToArray();
      return Task.FromResult(new PagedResultDto<DocumentSnapshot>(rows, request.Page, request.PageSize, rows.Length));
    }

    public Task<DocumentRecord?> FindAsync(
      EntityId documentId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Documents.FirstOrDefault(document =>
        document.Id == documentId &&
        document.OrganizationId == organizationId &&
        (includeArchived || !document.IsDeleted)));
    }

    public async Task<DocumentSnapshot?> FindSnapshotAsync(
      EntityId documentId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      var document = await FindAsync(documentId, organizationId, includeArchived, cancellationToken)
        .ConfigureAwait(false);
      return document is null ? null : new DocumentSnapshot(document);
    }

    public Task AddAsync(DocumentRecord document, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Documents.Add(document);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(DocumentRecord document, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingFileStorageProvider : IFileStorageProvider
  {
    private readonly Dictionary<string, StoredFile> storedFiles = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, StoredFile> StoredFiles => storedFiles;

    public async Task<StoredFileDescriptor> SaveAsync(
      FileStorageRequest request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      using var memory = new MemoryStream();
      await request.Content.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
      var storageKey = $"documents/{Guid.NewGuid():N}/{request.FileName}";
      storedFiles[storageKey] = new StoredFile(request.ContentType, memory.ToArray());
      return new StoredFileDescriptor(
        request.OrganizationId,
        storageKey,
        request.FileName,
        request.ContentType,
        memory.Length,
        DateTimeOffset.UtcNow,
        request.Metadata);
    }

    public Task<Stream> OpenReadAsync(
      OrganizationId organizationId,
      string storageKey,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<Stream>(new MemoryStream(storedFiles[storageKey].Content));
    }

    public Task DeleteAsync(
      OrganizationId organizationId,
      string storageKey,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      storedFiles.Remove(storageKey);
      return Task.CompletedTask;
    }
  }

  private sealed record StoredFile(string ContentType, byte[] Content);

  private sealed class FixedPermissionService : IPermissionService
  {
    private readonly HashSet<string> permissions;

    public FixedPermissionService(IEnumerable<string> permissions)
    {
      this.permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      string permissionCode,
      CancellationToken cancellationToken = default) =>
      AuthorizeAsync(new PermissionRequirement(permissionCode), cancellationToken);

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      PermissionRequirement requirement,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var organizationId = OrganizationId.New();
      var granted = permissions.Contains(PermissionCodes.Wildcard) ||
        permissions.Contains(requirement.PermissionCode);
      return ValueTask.FromResult(
        granted
          ? PermissionEvaluationResult.Granted(requirement, organizationId)
          : PermissionEvaluationResult.Denied(requirement, PermissionEvaluationFailure.PermissionDenied, organizationId));
    }

    public ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult<IReadOnlySet<string>>(permissions);
    }
  }

  private sealed class FixedActiveOrganizationContextResolver : IActiveOrganizationContextResolver
  {
    private readonly ActiveOrganizationContext context;

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId)
    {
      var membership = new OrganizationMembership(organizationId, permissionCodes: [PermissionCodes.Wildcard]);
      var user = new AuthenticatedUser(UserId.New(), "admin@alsappan.local", "Paulo", [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(CancellationToken cancellationToken = default) =>
      ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
  }

  private sealed class RecordingAuditWriter : IAuditWriter
  {
    public List<AuditEntryDraft> Entries { get; } = [];

    public Task WriteAsync(AuditEntryDraft entry, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Entries.Add(entry);
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingOutboxWriter : IModuleEventOutboxWriter
  {
    public List<ModuleEventEnvelope> Envelopes { get; } = [];

    public Task EnqueueAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Envelopes.Add(envelope);
      return Task.CompletedTask;
    }
  }
}
