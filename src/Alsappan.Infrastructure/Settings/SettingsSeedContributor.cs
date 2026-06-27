using Alsappan.Application.Common.Seeding;
using Alsappan.Domain.Settings;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Settings;

public sealed class SettingsSeedContributor : IDatabaseSeedContributor
{
  public string Name => "settings-defaults";

  public string Version => "2026.06.27";

  public async Task SeedAsync(
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(serviceProvider);

    var dbContext = serviceProvider.GetRequiredService<AlsappanDbContext>();
    var repository = serviceProvider.GetRequiredService<Application.Settings.Repositories.ISettingsRepository>();
    var clock = serviceProvider.GetRequiredService<TimeProvider>();
    var now = clock.GetUtcNow();
    var organizations = await dbContext.IdentityOrganizations
      .IgnoreQueryFilters()
      .Where(organization => organization.DeletedAt == null)
      .Select(organization => organization.Id)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    foreach (var organizationId in organizations)
    {
      await repository.GetOrCreateSettingsAsync(organizationId, now, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
      await repository.EnsureCatalogDefaultsAsync(organizationId, now, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
    }
  }
}
