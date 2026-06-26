using System.Globalization;

namespace Alsappan.Domain.Common.ValueObjects;

public readonly record struct BusinessDate
{
  public BusinessDate(DateOnly value)
  {
    if (value == default)
    {
      throw new ArgumentException("Date cannot be the default value.", nameof(value));
    }

    Value = value;
  }

  public DateOnly Value { get; }

  public static BusinessDate FromDateTime(DateTime value) => new(DateOnly.FromDateTime(value));

  public override string ToString() => Value.ToString("O", CultureInfo.InvariantCulture);
}
