namespace Alsappan.Domain.Common.Identifiers;

public readonly record struct OrganizationId
{
  public OrganizationId(Guid value)
  {
    if (value == Guid.Empty)
    {
      throw new ArgumentException("Organization id cannot be empty.", nameof(value));
    }

    Value = value;
  }

  public Guid Value { get; }

  public static OrganizationId New() => new(Guid.NewGuid());

  public static OrganizationId Parse(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    return new OrganizationId(Guid.Parse(value));
  }

  public override string ToString() => Value.ToString("D");
}
