namespace Alsappan.Domain.Common.Identifiers;

public readonly record struct EntityId
{
  public EntityId(Guid value)
  {
    if (value == Guid.Empty)
    {
      throw new ArgumentException("Entity id cannot be empty.", nameof(value));
    }

    Value = value;
  }

  public Guid Value { get; }

  public static EntityId New() => new(Guid.NewGuid());

  public static EntityId Parse(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    return new EntityId(Guid.Parse(value));
  }

  public override string ToString() => Value.ToString("D");
}
