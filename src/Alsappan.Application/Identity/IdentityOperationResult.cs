namespace Alsappan.Application.Identity;

#pragma warning disable CA1000

public enum IdentityOperationFailure
{
  Validation,
  Unauthorized,
  Forbidden,
  NotFound,
  Conflict
}

public record IdentityOperationResult(
  bool Succeeded,
  IdentityOperationFailure? Failure = null,
  IDictionary<string, string[]>? Errors = null)
{
  public static IdentityOperationResult Success() => new(true);

  public static IdentityOperationResult Failed(
    IdentityOperationFailure failure,
    IDictionary<string, string[]>? errors = null) =>
    new(false, failure, errors);
}

public sealed record IdentityOperationResult<TValue>(
  bool Succeeded,
  TValue? Value = default,
  IdentityOperationFailure? Failure = null,
  IDictionary<string, string[]>? Errors = null)
{
  public static IdentityOperationResult<TValue> Success(TValue value) =>
    new(true, value);

  public static IdentityOperationResult<TValue> Failed(
    IdentityOperationFailure failure,
    IDictionary<string, string[]>? errors = null) =>
    new(false, default, failure, errors);
}
