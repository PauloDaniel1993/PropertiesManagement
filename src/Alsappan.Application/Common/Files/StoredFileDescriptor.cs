using System.Collections.ObjectModel;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Files;

public sealed record StoredFileDescriptor
{
  public StoredFileDescriptor(
    OrganizationId organizationId,
    string storageKey,
    string fileName,
    string contentType,
    long length,
    DateTimeOffset storedAt,
    IReadOnlyDictionary<string, string>? metadata = null)
  {
    if (length < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(length), "File length cannot be negative.");
    }

    if (storedAt == default)
    {
      throw new ArgumentException("Stored timestamp is required.", nameof(storedAt));
    }

    OrganizationId = organizationId;
    StorageKey = Required(storageKey, nameof(storageKey));
    FileName = Required(fileName, nameof(fileName));
    ContentType = Required(contentType, nameof(contentType));
    Length = length;
    StoredAt = storedAt;
    Metadata = Copy(metadata);
  }

  public OrganizationId OrganizationId { get; }

  public string StorageKey { get; }

  public string FileName { get; }

  public string ContentType { get; }

  public long Length { get; }

  public DateTimeOffset StoredAt { get; }

  public IReadOnlyDictionary<string, string> Metadata { get; }

  private static ReadOnlyDictionary<string, string> Copy(IReadOnlyDictionary<string, string>? source)
  {
    if (source is null || source.Count == 0)
    {
      return ReadOnlyDictionary<string, string>.Empty;
    }

    var copy = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var (key, value) in source)
    {
      copy[Required(key, "metadata key")] = value;
    }

    return new ReadOnlyDictionary<string, string>(copy);
  }

  private static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    return value.Trim();
  }
}
