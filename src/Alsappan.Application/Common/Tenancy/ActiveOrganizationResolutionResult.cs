namespace Alsappan.Application.Common.Tenancy;

public enum ActiveOrganizationResolutionFailure
{
  Unauthenticated,
  NoMemberships,
  InvalidRequestedOrganization,
  RequestedOrganizationNotInMemberships,
  MultipleActiveMemberships,
  MultipleMembershipsRequireSelection
}

public sealed record ActiveOrganizationResolutionResult
{
  private ActiveOrganizationResolutionResult(
    ActiveOrganizationContext? context,
    ActiveOrganizationResolutionFailure? failure,
    string? detail)
  {
    Context = context;
    Failure = failure;
    Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
  }

  public bool Succeeded => Context is not null;

  public ActiveOrganizationContext? Context { get; }

  public ActiveOrganizationResolutionFailure? Failure { get; }

  public string? Detail { get; }

  public static ActiveOrganizationResolutionResult Success(ActiveOrganizationContext context)
  {
    ArgumentNullException.ThrowIfNull(context);
    return new ActiveOrganizationResolutionResult(context, null, null);
  }

  public static ActiveOrganizationResolutionResult Denied(
    ActiveOrganizationResolutionFailure failure,
    string? detail = null) =>
    new(null, failure, detail);
}
