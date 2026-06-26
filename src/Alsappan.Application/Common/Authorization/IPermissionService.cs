namespace Alsappan.Application.Common.Authorization;

public interface IPermissionService
{
  ValueTask<PermissionEvaluationResult> AuthorizeAsync(
    string permissionCode,
    CancellationToken cancellationToken = default);

  ValueTask<PermissionEvaluationResult> AuthorizeAsync(
    PermissionRequirement requirement,
    CancellationToken cancellationToken = default);

  ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default);
}
