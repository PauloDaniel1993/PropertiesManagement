using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Documents;

namespace Alsappan.Application.Documents.Repositories;

public sealed record DocumentSnapshot(DocumentRecord Document);

public interface IDocumentRepository
{
  Task<PagedResultDto<DocumentSnapshot>> ListAsync(
    DocumentListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<DocumentRecord?> FindAsync(
    EntityId documentId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<DocumentSnapshot?> FindSnapshotAsync(
    EntityId documentId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task AddAsync(DocumentRecord document, CancellationToken cancellationToken = default);

  Task UpdateAsync(DocumentRecord document, CancellationToken cancellationToken = default);
}
