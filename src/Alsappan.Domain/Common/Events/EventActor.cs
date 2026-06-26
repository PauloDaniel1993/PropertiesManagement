using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Domain.Common.Events;

public sealed record EventActor
{
  private const string SystemActorKind = "system";
  private const string UserActorKind = "user";
  private const string ResidentActorKind = "resident";

  public EventActor(string actorKind, UserId? userId = null, string? displayName = null)
  {
    ActorKind = Required(actorKind, nameof(actorKind));
    UserId = userId;
    DisplayName = Optional(displayName);
  }

  public string ActorKind { get; }

  public UserId? UserId { get; }

  public string? DisplayName { get; }

  public static EventActor System(string? displayName = null) => new(SystemActorKind, null, displayName);

  public static EventActor User(UserId userId, string? displayName = null) => new(UserActorKind, userId, displayName);

  public static EventActor Resident(UserId userId, string? displayName = null) => new(ResidentActorKind, userId, displayName);

  private static string Required(string value, string parameterName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
#pragma warning disable CA1308 // Event actor kinds are stable lowercase codes.
    return value.Trim().ToLowerInvariant();
#pragma warning restore CA1308
  }

  private static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
