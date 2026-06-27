using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Identity;

#pragma warning disable CA1711

public sealed class IdentityPermission : Entity<EntityId>
{
  private IdentityPermission()
  {
  }

  private IdentityPermission(
    EntityId id,
    string code,
    string module,
    string action,
    string? description)
    : base(id)
  {
    Code = IdentityCode.NormalizeCode(code);
    Module = IdentityCode.NormalizeCode(module);
    Action = IdentityCode.NormalizeCode(action);
    Description = IdentityCode.Optional(description);
  }

  public string Code { get; private set; } = string.Empty;

  public string Module { get; private set; } = string.Empty;

  public string Action { get; private set; } = string.Empty;

  public string? Description { get; private set; }

  public static IdentityPermission Create(
    EntityId id,
    string code,
    string module,
    string action,
    string? description = null) =>
    new(id, code, module, action, description);
}
