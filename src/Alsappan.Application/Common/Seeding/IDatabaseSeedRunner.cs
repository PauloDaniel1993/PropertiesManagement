namespace Alsappan.Application.Common.Seeding;

public interface IDatabaseSeedRunner
{
  Task<SeedExecutionResult> RunAsync(CancellationToken cancellationToken = default);
}
