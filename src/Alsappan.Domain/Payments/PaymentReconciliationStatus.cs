namespace Alsappan.Domain.Payments;

public enum PaymentReconciliationStatus
{
  NotRequired = 0,
  Pending = 1,
  Matched = 2,
  Failed = 3,
  ManualReview = 4
}
