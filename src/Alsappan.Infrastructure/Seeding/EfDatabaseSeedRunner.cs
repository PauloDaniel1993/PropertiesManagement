using Alsappan.Application.Common.Seeding;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Seeding;

public sealed class EfDatabaseSeedRunner : IDatabaseSeedRunner
{
  private readonly AlsappanDbContext _dbContext;
  private readonly IEnumerable<IDatabaseSeedContributor> _contributors;
  private readonly IServiceProvider _serviceProvider;
  private readonly TimeProvider _clock;

  public EfDatabaseSeedRunner(
    AlsappanDbContext dbContext,
    IEnumerable<IDatabaseSeedContributor> contributors,
    IServiceProvider serviceProvider,
    TimeProvider? clock = null)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    _clock = clock ?? TimeProvider.System;
  }

  public async Task<SeedExecutionResult> RunAsync(CancellationToken cancellationToken = default)
  {
    var executed = new List<string>();
    var skipped = new List<string>();

    foreach (var contributor in _contributors
      .OrderBy(contributor => contributor.Name, StringComparer.Ordinal)
      .ThenBy(contributor => contributor.Version, StringComparer.Ordinal))
    {
      var name = Required(contributor.Name, nameof(contributor.Name));
      var version = Required(contributor.Version, nameof(contributor.Version));
      var contributorKey = $"{name}@{version}";

      var alreadyExecuted = await _dbContext.SeedHistoryRecords
        .AnyAsync(
          record => record.Name == name && record.Version == version,
          cancellationToken)
        .ConfigureAwait(false);

      if (alreadyExecuted)
      {
        skipped.Add(contributorKey);
        continue;
      }

      await contributor.SeedAsync(_serviceProvider, cancellationToken).ConfigureAwait(false);
      _dbContext.SeedHistoryRecords.Add(
        SeedHistoryRecord.Create(name, version, _clock.GetUtcNow()));
      await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      executed.Add(contributorKey);
    }

    return new SeedExecutionResult(executed, skipped);
  }

  private static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    return value.Trim();
  }
}
