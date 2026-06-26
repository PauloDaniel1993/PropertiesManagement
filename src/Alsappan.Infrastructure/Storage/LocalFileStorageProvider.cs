using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Files;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Infrastructure.Storage;

public sealed class LocalFileStorageProvider : IFileStorageProvider
{
  private readonly string _rootPath;
  private readonly TimeProvider _clock;

  public LocalFileStorageProvider(StorageOptions storageOptions, TimeProvider? clock = null)
  {
    ArgumentNullException.ThrowIfNull(storageOptions);

    _rootPath = Path.GetFullPath(storageOptions.LocalPath);
    _clock = clock ?? TimeProvider.System;
  }

  public async Task<StoredFileDescriptor> SaveAsync(
    FileStorageRequest request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var storedAt = _clock.GetUtcNow();
    var fileName = SanitizeFileName(request.FileName);
    var storageKey = CreateStorageKey(request.OrganizationId, storedAt, fileName);
    var fullPath = ResolvePath(request.OrganizationId, storageKey);
    var directory = Path.GetDirectoryName(fullPath) ??
      throw new InvalidOperationException("Could not resolve storage directory.");

    Directory.CreateDirectory(directory);

    using (var destination = new FileStream(
      fullPath,
      FileMode.CreateNew,
      FileAccess.Write,
      FileShare.None,
      bufferSize: 81_920,
      useAsync: true))
    {
      await request.Content.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    var length = new FileInfo(fullPath).Length;
    return new StoredFileDescriptor(
      request.OrganizationId,
      storageKey,
      fileName,
      request.ContentType,
      length,
      storedAt,
      request.Metadata);
  }

  public Task<Stream> OpenReadAsync(
    OrganizationId organizationId,
    string storageKey,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var fullPath = ResolvePath(organizationId, storageKey);
    Stream stream = new FileStream(
      fullPath,
      FileMode.Open,
      FileAccess.Read,
      FileShare.Read,
      bufferSize: 81_920,
      useAsync: true);

    return Task.FromResult(stream);
  }

  public Task DeleteAsync(
    OrganizationId organizationId,
    string storageKey,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var fullPath = ResolvePath(organizationId, storageKey);
    if (File.Exists(fullPath))
    {
      File.Delete(fullPath);
    }

    return Task.CompletedTask;
  }

  private static string CreateStorageKey(
    OrganizationId organizationId,
    DateTimeOffset storedAt,
    string fileName) =>
    string.Join(
      '/',
      "organizations",
      organizationId.ToString(),
      storedAt.UtcDateTime.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture),
      storedAt.UtcDateTime.ToString("MM", System.Globalization.CultureInfo.InvariantCulture),
      storedAt.UtcDateTime.ToString("dd", System.Globalization.CultureInfo.InvariantCulture),
      $"{Guid.NewGuid():N}-{fileName}");

  private string ResolvePath(OrganizationId organizationId, string storageKey)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

    var expectedPrefix = $"organizations/{organizationId}/";
    if (!storageKey.StartsWith(expectedPrefix, StringComparison.Ordinal))
    {
      throw new UnauthorizedAccessException("Storage key does not belong to the requested organization.");
    }

    var relativePath = storageKey.Replace('/', Path.DirectorySeparatorChar);
    var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
    var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
      ? _rootPath
      : _rootPath + Path.DirectorySeparatorChar;

    if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
    {
      throw new UnauthorizedAccessException("Storage key escapes the configured storage root.");
    }

    return fullPath;
  }

  private static string SanitizeFileName(string fileName)
  {
    var invalidCharacters = Path.GetInvalidFileNameChars();
    var sanitized = new string(fileName
      .Trim()
      .Select(character => invalidCharacters.Contains(character) ? '_' : character)
      .ToArray());

    while (sanitized.Contains("..", StringComparison.Ordinal))
    {
      sanitized = sanitized.Replace("..", "_", StringComparison.Ordinal);
    }

    sanitized = sanitized.Trim('.', '_', ' ');

    if (string.IsNullOrWhiteSpace(sanitized))
    {
      return "file";
    }

    return sanitized.Length <= 160 ? sanitized : sanitized[..160];
  }
}
