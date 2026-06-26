namespace Alsappan.Domain.Common.Events;

[Flags]
public enum ModuleEventConsumer
{
  None = 0,
  Audit = 1,
  Timeline = 2,
  Notifications = 4,
  DashboardProjection = 8,
  ResidentPortal = 16,
  TenantBackgroundProcessing = 32,
}
