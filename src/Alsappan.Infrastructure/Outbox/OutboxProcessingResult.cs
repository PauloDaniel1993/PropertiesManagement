namespace Alsappan.Infrastructure.Outbox;

public sealed record OutboxProcessingResult(
  int ProcessedCount,
  int FailedCount,
  int PendingCount);
