using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Common.Authorization;

public sealed record PermissionRequirement
{
  public PermissionRequirement(string permissionCode, OrganizationId? organizationId = null)
  {
    PermissionCode = PermissionCodes.Normalize(permissionCode);
    OrganizationId = organizationId;
  }

  public string PermissionCode { get; }

  public OrganizationId? OrganizationId { get; }
}

public enum PermissionEvaluationFailure
{
  Unauthenticated,
  NoActiveOrganization,
  OrganizationMismatch,
  PermissionDenied
}

public sealed record PermissionEvaluationResult
{
  private PermissionEvaluationResult(
    PermissionRequirement requirement,
    OrganizationId? activeOrganizationId,
    bool isGranted,
    PermissionEvaluationFailure? failure)
  {
    Requirement = requirement;
    ActiveOrganizationId = activeOrganizationId;
    IsGranted = isGranted;
    Failure = failure;
  }

  public PermissionRequirement Requirement { get; }

  public OrganizationId? ActiveOrganizationId { get; }

  public bool IsGranted { get; }

  public PermissionEvaluationFailure? Failure { get; }

  public static PermissionEvaluationResult Granted(
    PermissionRequirement requirement,
    OrganizationId activeOrganizationId)
  {
    ArgumentNullException.ThrowIfNull(requirement);
    return new PermissionEvaluationResult(requirement, activeOrganizationId, true, null);
  }

  public static PermissionEvaluationResult Denied(
    PermissionRequirement requirement,
    PermissionEvaluationFailure failure,
    OrganizationId? activeOrganizationId = null)
  {
    ArgumentNullException.ThrowIfNull(requirement);
    return new PermissionEvaluationResult(requirement, activeOrganizationId, false, failure);
  }
}
