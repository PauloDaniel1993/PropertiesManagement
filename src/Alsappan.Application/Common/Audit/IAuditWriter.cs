namespace Alsappan.Application.Common.Audit;

public interface IAuditWriter
{
  Task WriteAsync(AuditEntryDraft entry, CancellationToken cancellationToken = default);
}
