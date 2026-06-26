namespace Alsappan.Domain.Common.Identifiers;

public readonly record struct UserId
{
  public UserId(Guid value)
  {
    if (value == Guid.Empty)
    {
      throw new ArgumentException("User id cannot be empty.", nameof(value));
    }

    Value = value;
  }

  public Guid Value { get; }

  public static UserId New() => new(Guid.NewGuid());

  public static UserId Parse(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    return new UserId(Guid.Parse(value));
  }

  public override string ToString() => Value.ToString("D");
}
