namespace Alsappan.Infrastructure.Seeding;

public sealed class SeedHistoryRecord
{
  private SeedHistoryRecord()
  {
  }

  private SeedHistoryRecord(string name, string version, DateTimeOffset executedAt)
  {
    Id = Guid.NewGuid();
    Name = Required(name, nameof(name));
    Version = Required(version, nameof(version));
    ExecutedAt = executedAt;
  }

  public Guid Id { get; private set; }

  public string Name { get; private set; } = string.Empty;

  public string Version { get; private set; } = string.Empty;

  public DateTimeOffset ExecutedAt { get; private set; }

  public static SeedHistoryRecord Create(
    string name,
    string version,
    DateTimeOffset executedAt)
  {
    if (executedAt == default)
    {
      throw new ArgumentException("Executed timestamp is required.", nameof(executedAt));
    }

    return new SeedHistoryRecord(name, version, executedAt);
  }

  private static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    return value.Trim();
  }
}
