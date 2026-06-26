using System.Collections.ObjectModel;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Files;

public sealed record FileStorageRequest
{
  public FileStorageRequest(
    OrganizationId organizationId,
    string fileName,
    string contentType,
    Stream content,
    IReadOnlyDictionary<string, string>? metadata = null)
  {
    OrganizationId = organizationId;
    FileName = Required(fileName, nameof(fileName));
    ContentType = Required(contentType, nameof(contentType));
    Content = content ?? throw new ArgumentNullException(nameof(content));
    Metadata = Copy(metadata);
  }

  public OrganizationId OrganizationId { get; }

  public string FileName { get; }

  public string ContentType { get; }

  public Stream Content { get; }

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
