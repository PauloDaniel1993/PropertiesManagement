using Alsappan.Application.Common.Audit;
using Alsappan.Infrastructure.Persistence;

namespace Alsappan.Infrastructure.Audit;

public sealed class EfAuditWriter : IAuditWriter
{
  private readonly AlsappanDbContext _dbContext;

  public EfAuditWriter(AlsappanDbContext dbContext)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task WriteAsync(
    AuditEntryDraft entry,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entry);

    _dbContext.AuditLogEntries.Add(AuditLogEntry.FromDraft(entry));
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
