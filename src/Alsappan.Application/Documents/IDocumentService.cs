using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Documents;

public interface IDocumentService
{
  Task<ApplicationOperationResult<PagedResultDto<DocumentListItemDto>>> ListAsync(
    DocumentListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DocumentDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DocumentDetailDto>> UploadAsync(
    DocumentUploadRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DocumentDetailDto>> UpdateAsync(
    Guid id,
    DocumentUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DocumentDetailDto>> UploadVersionAsync(
    Guid id,
    DocumentVersionUploadRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DocumentDownloadDto>> DownloadAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DocumentDownloadDto>> DownloadVersionAsync(
    Guid id,
    int versionNumber,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DocumentDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetCategoryOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetAllowedFileTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
