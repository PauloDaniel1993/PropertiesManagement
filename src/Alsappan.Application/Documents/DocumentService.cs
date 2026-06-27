using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Files;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Documents.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Documents;

namespace Alsappan.Application.Documents;

public sealed class DocumentService : IDocumentService
{
  private readonly IDocumentRepository documentRepository;
  private readonly IFileStorageProvider fileStorageProvider;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public DocumentService(
    IDocumentRepository documentRepository,
    IFileStorageProvider fileStorageProvider,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider? timeProvider = null)
  {
    this.documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
    this.fileStorageProvider = fileStorageProvider ?? throw new ArgumentNullException(nameof(fileStorageProvider));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<PagedResultDto<DocumentListItemDto>>> ListAsync(
    DocumentListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<DocumentListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var page = await documentRepository.ListAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    var items = new List<DocumentListItemDto>(page.Items.Count);
    foreach (var snapshot in page.Items)
    {
      if (await CanReadLinkedEntitiesAsync(snapshot.Document.Links, cancellationToken).ConfigureAwait(false))
      {
        items.Add(ToListItem(snapshot.Document, request.Locale));
      }
    }

    return ApplicationOperationResult<PagedResultDto<DocumentListItemDto>>.Success(
      new PagedResultDto<DocumentListItemDto>(items, page.Page, page.PageSize, items.Count == page.Items.Count ? page.TotalItems : items.Count));
  }

  public async Task<ApplicationOperationResult<DocumentDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await documentRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    if (snapshot is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!await CanReadLinkedEntitiesAsync(snapshot.Document.Links, cancellationToken).ConfigureAwait(false))
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    return ApplicationOperationResult<DocumentDetailDto>.Success(ToDetail(snapshot.Document, locale));
  }

  public async Task<ApplicationOperationResult<DocumentDetailDto>> UploadAsync(
    DocumentUploadRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var validation = ValidateUpload(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Invalid(validation);
    }

    var category = ParseCategory(request.Category);
    var linkDrafts = ToLinkDrafts(request.Links);
    var linkAccessValidation = await ValidateLinkReadAccessAsync(linkDrafts, cancellationToken)
      .ConfigureAwait(false);
    if (linkAccessValidation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Invalid(linkAccessValidation);
    }

    var storedFile = await SaveFileAsync(
        context.OrganizationId,
        request.FileName,
        request.ContentType,
        request.Content,
        cancellationToken)
      .ConfigureAwait(false);
    var now = timeProvider.GetUtcNow();
    var document = DocumentRecord.Create(
      EntityId.New(),
      context.OrganizationId,
      category,
      request.Title,
      request.Description,
      storedFile.FileName,
      storedFile.ContentType,
      storedFile.Length,
      storedFile.StorageKey,
      request.VersionNotes,
      linkDrafts,
      now,
      context.UserId);

    await documentRepository.AddAsync(document, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("document.uploaded", document, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<DocumentDetailDto>.Success(ToDetail(document, locale));
  }

  public async Task<ApplicationOperationResult<DocumentDetailDto>> UpdateAsync(
    Guid id,
    DocumentUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateMetadata(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var document = await documentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (document is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var concurrency = EnsureCurrentConcurrencyToken(document, request.ConcurrencyToken);
    if (concurrency is not null)
    {
      return concurrency;
    }

    var linkDrafts = ToLinkDrafts(request.Links);
    var linkAccessValidation = await ValidateLinkReadAccessAsync(linkDrafts, cancellationToken)
      .ConfigureAwait(false);
    if (linkAccessValidation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Invalid(linkAccessValidation);
    }

    document.UpdateMetadata(
      ParseCategory(request.Category),
      request.Title,
      request.Description,
      linkDrafts,
      timeProvider.GetUtcNow(),
      context.UserId);

    await documentRepository.UpdateAsync(document, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("document.updated", document, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<DocumentDetailDto>.Success(ToDetail(document, locale));
  }

  public async Task<ApplicationOperationResult<DocumentDetailDto>> UploadVersionAsync(
    Guid id,
    DocumentVersionUploadRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateVersionUpload(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var document = await documentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (document is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var storedFile = await SaveFileAsync(
        context.OrganizationId,
        request.FileName,
        request.ContentType,
        request.Content,
        cancellationToken)
      .ConfigureAwait(false);
    document.AddVersion(
      storedFile.FileName,
      storedFile.ContentType,
      storedFile.Length,
      storedFile.StorageKey,
      request.Notes,
      timeProvider.GetUtcNow(),
      context.UserId);

    await documentRepository.UpdateAsync(document, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("document.version-uploaded", document, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<DocumentDetailDto>.Success(ToDetail(document, locale));
  }

  public async Task<ApplicationOperationResult<DocumentDownloadDto>> DownloadAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var document = await documentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (document is null)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!await CanReadLinkedEntitiesAsync(document.Links, cancellationToken).ConfigureAwait(false))
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var stream = await fileStorageProvider.OpenReadAsync(
        context.OrganizationId,
        document.CurrentStorageKey,
        cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("document.downloaded", document, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<DocumentDownloadDto>.Success(new DocumentDownloadDto(
      document.CurrentFileName,
      document.CurrentContentType,
      document.CurrentSizeBytes,
      stream));
  }

  public async Task<ApplicationOperationResult<DocumentDownloadDto>> DownloadVersionAsync(
    Guid id,
    int versionNumber,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).Concat(ValidateVersionNumber(versionNumber)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var document = await documentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (document is null)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!await CanReadLinkedEntitiesAsync(document.Links, cancellationToken).ConfigureAwait(false))
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var version = document.Versions.FirstOrDefault(candidate => candidate.VersionNumber == versionNumber);
    if (version is null)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var stream = await fileStorageProvider.OpenReadAsync(
        context.OrganizationId,
        version.StorageKey,
        cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("document.version-downloaded", document, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<DocumentDownloadDto>.Success(new DocumentDownloadDto(
      version.FileName,
      version.ContentType,
      version.SizeBytes,
      stream));
  }

  public async Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var document = await documentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (document is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    document.Archive(timeProvider.GetUtcNow(), context.UserId);
    await documentRepository.UpdateAsync(document, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("document.archived", document, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<DocumentDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Documents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var document = await documentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    if (document is null)
    {
      return ApplicationOperationResult<DocumentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    document.Restore(timeProvider.GetUtcNow(), context.UserId);
    await documentRepository.UpdateAsync(document, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("document.restored", document, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<DocumentDetailDto>.Success(ToDetail(document, locale));
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetCategoryOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(DocumentCatalog.GetCategoryOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(DocumentCatalog.GetStatusOptions(locale));
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetAllowedFileTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(DocumentCatalog.GetAllowedFileTypeOptions(locale));
  }

  private async Task<ActiveOrganizationContext?> AuthorizeContextAsync(
    string permissionCode,
    CancellationToken cancellationToken)
  {
    var permission = await permissionService.AuthorizeAsync(permissionCode, cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return null;
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    return context.Succeeded ? context.Context : null;
  }

  private async Task<IReadOnlyList<ValidationFailure>> ValidateLinkReadAccessAsync(
    IEnumerable<DocumentLinkDraft> links,
    CancellationToken cancellationToken)
  {
    var failures = new List<ValidationFailure>();
    foreach (var entityType in links
      .Select(link => DocumentCode.NormalizeCode(link.EntityType))
      .Distinct(StringComparer.Ordinal))
    {
      if (TryGetLinkedEntityModule(entityType, out var module) &&
        await HasPermissionAsync(PermissionCodes.Read(module), cancellationToken).ConfigureAwait(false))
      {
        continue;
      }

      failures.Add(new ValidationFailure(nameof(links), "validation.documentLinkAccess"));
    }

    return failures;
  }

  private async Task<bool> CanReadLinkedEntitiesAsync(
    IEnumerable<DocumentLink> links,
    CancellationToken cancellationToken)
  {
    foreach (var entityType in links
      .Select(link => DocumentCode.NormalizeCode(link.EntityType))
      .Distinct(StringComparer.Ordinal))
    {
      if (!TryGetLinkedEntityModule(entityType, out var module) ||
        !await HasPermissionAsync(PermissionCodes.Read(module), cancellationToken).ConfigureAwait(false))
      {
        return false;
      }
    }

    return true;
  }

  private async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken)
  {
    var permission = await permissionService.AuthorizeAsync(permissionCode, cancellationToken)
      .ConfigureAwait(false);
    return permission.IsGranted;
  }

  private static bool TryGetLinkedEntityModule(string entityType, out string module)
  {
    module = DocumentCode.NormalizeCode(entityType) switch
    {
      "contract" => PermissionModules.Contracts,
      "inspection" => PermissionModules.Inspections,
      "occurrence" => PermissionModules.Occurrences,
      "payment" => PermissionModules.Payments,
      "pet" => PermissionModules.Pets,
      "property" => PermissionModules.Properties,
      "resident" => PermissionModules.Residents,
      "utility-account" => PermissionModules.UtilityAccounts,
      "vehicle" => PermissionModules.Vehicles,
      _ => string.Empty
    };

    return !string.IsNullOrWhiteSpace(module);
  }

  private async Task<StoredFileDescriptor> SaveFileAsync(
    OrganizationId organizationId,
    string fileName,
    string contentType,
    Stream content,
    CancellationToken cancellationToken) =>
    await fileStorageProvider.SaveAsync(
        new FileStorageRequest(
          organizationId,
          fileName,
          contentType,
          content,
          new Dictionary<string, string>(StringComparer.Ordinal)
          {
            ["module"] = "documents"
          }),
        cancellationToken)
      .ConfigureAwait(false);

  private static DocumentLinkDraft[] ToLinkDrafts(IReadOnlyList<DocumentLinkRequestDto>? links)
  {
    if (links is null || links.Count == 0)
    {
      return [];
    }

    return links
      .Where(link => link.EntityId != Guid.Empty && !string.IsNullOrWhiteSpace(link.EntityType))
      .Select(link => new DocumentLinkDraft(
        DocumentCode.NormalizeCode(link.EntityType),
        new EntityId(link.EntityId),
        link.Label))
      .ToArray();
  }

  private static IEnumerable<ValidationFailure> ValidateUpload(DocumentUploadRequestDto request)
  {
    foreach (var failure in ValidateMetadata(request.Category, request.Title, request.Links))
    {
      yield return failure;
    }

    foreach (var failure in ValidateFile(request.FileName, request.ContentType, request.SizeBytes))
    {
      yield return failure;
    }
  }

  private static IEnumerable<ValidationFailure> ValidateVersionUpload(DocumentVersionUploadRequestDto request) =>
    ValidateFile(request.FileName, request.ContentType, request.SizeBytes);

  private static IEnumerable<ValidationFailure> ValidateMetadata(DocumentUpdateRequestDto request) =>
    ValidateMetadata(request.Category, request.Title, request.Links);

  private static IEnumerable<ValidationFailure> ValidateMetadata(
    string category,
    string title,
    IReadOnlyList<DocumentLinkRequestDto>? links)
  {
    if (!DocumentCatalog.TryParseCategory(category, out _))
    {
      yield return new ValidationFailure(nameof(category), "validation.documentCategory");
    }

    if (string.IsNullOrWhiteSpace(title))
    {
      yield return new ValidationFailure(nameof(title), ValidationMessageKeys.Required);
    }

    foreach (var link in links ?? [])
    {
      if (string.IsNullOrWhiteSpace(link.EntityType) || !DocumentCatalog.IsSupportedEntityType(link.EntityType))
      {
        yield return new ValidationFailure(nameof(links), "validation.documentLink");
      }

      if (link.EntityId == Guid.Empty)
      {
        yield return new ValidationFailure(nameof(links), ValidationMessageKeys.InvalidId);
      }
    }
  }

  private static IEnumerable<ValidationFailure> ValidateFile(
    string fileName,
    string contentType,
    long sizeBytes)
  {
    if (sizeBytes <= 0 || sizeBytes > DocumentCatalog.MaxFileSizeBytes)
    {
      yield return new ValidationFailure(nameof(sizeBytes), "validation.fileSize");
    }

    if (!DocumentCatalog.IsAllowedFile(fileName, contentType))
    {
      yield return new ValidationFailure(nameof(fileName), "validation.fileType");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id)
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateVersionNumber(int versionNumber)
  {
    if (versionNumber < 1)
    {
      yield return new ValidationFailure(nameof(versionNumber), ValidationMessageKeys.MinValue);
    }
  }

  private static DocumentCategory ParseCategory(string value) =>
    DocumentCatalog.TryParseCategory(value, out var category)
      ? category
      : throw new ArgumentException("Invalid document category.", nameof(value));

  private static ApplicationOperationResult<DocumentDetailDto>? EnsureCurrentConcurrencyToken(
    DocumentRecord document,
    string? concurrencyToken) =>
    string.IsNullOrWhiteSpace(concurrencyToken) ||
    !string.Equals(concurrencyToken.Trim(), document.ConcurrencyToken.Value, StringComparison.Ordinal)
      ? ApplicationOperationResult<DocumentDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["concurrencyToken"] = ["validation.concurrency"] })
      : null;

  private async Task WriteMutationSideEffectsAsync(
    string action,
    DocumentRecord document,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    var subject = EntityReference.FromGuid("document", document.Id.Value, document.Title);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["category"] = DocumentCatalog.ToCategoryCode(document.Category),
      ["status"] = DocumentCatalog.ToStatusCode(document.Status),
      ["fileName"] = document.CurrentFileName,
      ["contentType"] = document.CurrentContentType,
      ["sizeBytes"] = document.CurrentSizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
    var related = document.Links
      .Select(link => EntityReference.FromGuid(link.EntityType, link.EntityId.Value, link.Label))
      .ToArray();
    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "documents",
      action,
      now,
      actor,
      subject,
      ModuleEventConsumer.Audit | ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications,
      data,
      related);

    await auditWriter.WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static DocumentListItemDto ToListItem(DocumentRecord document, string? locale)
  {
    return new DocumentListItemDto(
      document.Id.Value,
      document.Title,
      document.Description,
      document.CurrentFileName,
      document.CurrentContentType,
      document.CurrentSizeBytes,
      DocumentCatalog.ToCategoryCode(document.Category),
      DocumentCatalog.GetCategoryLabel(document.Category, locale),
      DocumentCatalog.GetStatusLabel(document.Status, locale),
      document.CurrentVersionNumber,
      document.CurrentUploadedAt,
      document.UpdatedAt,
      document.IsDeleted,
      document.Links.Select(ToLinkDto).ToArray(),
      document.ConcurrencyToken.Value);
  }

  private static DocumentDetailDto ToDetail(DocumentRecord document, string? locale)
  {
    var listItem = ToListItem(document, locale);
    var id = Uri.EscapeDataString(document.Id.Value.ToString("D"));
    return new DocumentDetailDto(
      listItem.Id,
      listItem.Title,
      listItem.Description,
      listItem.FileName,
      listItem.ContentType,
      listItem.SizeBytes,
      listItem.Category,
      listItem.CategoryLabel,
      listItem.Status,
      listItem.CurrentVersionNumber,
      listItem.UploadedAt,
      document.CreatedAt,
      listItem.UpdatedAt,
      document.DeletedAt,
      listItem.Links,
      document.Versions
        .OrderByDescending(version => version.VersionNumber)
        .Select(version => new DocumentVersionDto(
          version.Id.Value,
          version.VersionNumber,
          version.FileName,
          version.ContentType,
          version.SizeBytes,
          version.CreatedAt,
          version.CreatedByUserId?.Value,
          version.Notes))
        .ToArray(),
      $"/v1/documents/{id}/download",
      $"/timeline?entityType=document&entityId={id}",
      $"/auditoria?entityType=document&entityId={id}",
      document.ConcurrencyToken.Value);
  }

  private static DocumentLinkDto ToLinkDto(DocumentLink link)
  {
    var id = Uri.EscapeDataString(link.EntityId.Value.ToString("D"));
    var entityType = DocumentCode.NormalizeCode(link.EntityType);
    return new DocumentLinkDto(entityType, link.EntityId.Value, link.Label, $"/{RouteSegment(entityType)}?id={id}");
  }

  private static string RouteSegment(string entityType) =>
    entityType switch
    {
      "property" => "imoveis",
      "contract" => "contratos",
      "resident" => "moradores",
      "payment" => "pagamentos",
      "utility-account" => "contas-de-consumo",
      "pet" => "pets",
      "vehicle" => "veiculos",
      "occurrence" => "ocorrencias",
      "inspection" => "vistorias",
      _ => "documentos"
    };
}
