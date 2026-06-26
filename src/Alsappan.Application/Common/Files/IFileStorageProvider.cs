using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Files;

public interface IFileStorageProvider
{
  Task<StoredFileDescriptor> SaveAsync(
    FileStorageRequest request,
    CancellationToken cancellationToken = default);

  Task<Stream> OpenReadAsync(
    OrganizationId organizationId,
    string storageKey,
    CancellationToken cancellationToken = default);

  Task DeleteAsync(
    OrganizationId organizationId,
    string storageKey,
    CancellationToken cancellationToken = default);
}
