namespace Alsappan.Application.Common.Seeding;

public interface IDatabaseSeedContributor
{
  string Name { get; }

  string Version { get; }

  Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
