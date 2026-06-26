namespace Alsappan.Domain.Common.Metadata;

public readonly record struct ConcurrencyToken
{
  public ConcurrencyToken(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    Value = value.Trim();
  }

  public string Value { get; }

  public static ConcurrencyToken New() => new(Guid.NewGuid().ToString("N"));

  public override string ToString() => Value;
}
