using System.Text;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Files;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Storage;

namespace Alsappan.Infrastructure.Tests.Storage;

public sealed class LocalFileStorageProviderTests : IDisposable
{
  private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"alsappan-storage-{Guid.NewGuid():N}");

  [Fact]
  public async Task SaveAsyncStoresFilesUnderOrganizationScopedKey()
  {
    var organizationId = OrganizationId.New();
    var provider = CreateProvider();
    using var content = new MemoryStream(Encoding.UTF8.GetBytes("contract"));

    var descriptor = await provider.SaveAsync(new FileStorageRequest(
      organizationId,
      " contract.pdf ",
      "application/pdf",
      content)).ConfigureAwait(true);

    Assert.StartsWith($"organizations/{organizationId}/", descriptor.StorageKey, StringComparison.Ordinal);
    Assert.Equal("contract.pdf", descriptor.FileName);
    Assert.Equal(8, descriptor.Length);

    using var stored = await provider.OpenReadAsync(organizationId, descriptor.StorageKey)
      .ConfigureAwait(true);
    using var reader = new StreamReader(stored, Encoding.UTF8);

    Assert.Equal("contract", await reader.ReadToEndAsync().ConfigureAwait(true));
  }

  [Fact]
  public async Task OpenReadAsyncRejectsCrossOrganizationStorageKeys()
  {
    var organizationId = OrganizationId.New();
    var otherOrganizationId = OrganizationId.New();
    var provider = CreateProvider();
    using var content = new MemoryStream(Encoding.UTF8.GetBytes("private"));

    var descriptor = await provider.SaveAsync(new FileStorageRequest(
      organizationId,
      "../private.txt",
      "text/plain",
      content)).ConfigureAwait(true);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      provider.OpenReadAsync(otherOrganizationId, descriptor.StorageKey)).ConfigureAwait(true);
    Assert.DoesNotContain("..", descriptor.FileName, StringComparison.Ordinal);
  }

  public void Dispose()
  {
    if (Directory.Exists(_rootPath))
    {
      Directory.Delete(_rootPath, recursive: true);
    }
  }

  private LocalFileStorageProvider CreateProvider() =>
    new(new StorageOptions { LocalPath = _rootPath });
}
