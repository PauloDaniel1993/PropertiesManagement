namespace Alsappan.Domain.Payments;

public enum PaymentStatus
{
  Pending = 0,
  Overdue = 1,
  PartiallyPaid = 2,
  Paid = 3,
  Cancelled = 4,
  Disputed = 5,
  Archived = 6
}
