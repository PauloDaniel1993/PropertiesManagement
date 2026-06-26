namespace Alsappan.Application.Common.Seeding;

public sealed record SeedExecutionResult(
  IReadOnlyCollection<string> ExecutedContributors,
  IReadOnlyCollection<string> SkippedContributors)
{
  public bool ExecutedAny => ExecutedContributors.Count > 0;
}
