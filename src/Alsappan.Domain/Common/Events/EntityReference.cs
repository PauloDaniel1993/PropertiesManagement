namespace Alsappan.Domain.Common.Events;

public sealed record EntityReference
{
  public EntityReference(string entityType, string entityId, string? displayName = null)
  {
    EntityType = Required(entityType, nameof(entityType));
    EntityId = Required(entityId, nameof(entityId));
    DisplayName = Optional(displayName);
  }

  public string EntityType { get; }

  public string EntityId { get; }

  public string? DisplayName { get; }

  public static EntityReference FromGuid(string entityType, Guid entityId, string? displayName = null)
  {
    if (entityId == Guid.Empty)
    {
      throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));
    }

    return new EntityReference(entityType, entityId.ToString("D"), displayName);
  }

  private static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    return value.Trim();
  }

  private static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
