namespace Alsappan.Domain.Common.ValueObjects;

public readonly record struct DateRange
{
  public DateRange(BusinessDate start, BusinessDate? end = null)
  {
    if (end.HasValue && end.Value.Value < start.Value)
    {
      throw new ArgumentException("Date range end cannot be before start.", nameof(end));
    }

    Start = start;
    End = end;
  }

  public BusinessDate Start { get; }

  public BusinessDate? End { get; }

  public bool IsOpenEnded => !End.HasValue;

  public bool Contains(BusinessDate date) =>
    date.Value >= Start.Value && (!End.HasValue || date.Value <= End.Value.Value);
}
