using Alsappan.Application.Common.Validation;

namespace Alsappan.Application.Common.Results;

public enum ApplicationOperationFailure
{
  None = 0,
  Validation = 1,
  Unauthorized = 2,
  Forbidden = 3,
  NotFound = 4,
  Conflict = 5
}

public class ApplicationOperationResult
{
  private protected ApplicationOperationResult(
    bool succeeded,
    ApplicationOperationFailure failure = ApplicationOperationFailure.None,
    IDictionary<string, string[]>? errors = null)
  {
    Succeeded = succeeded;
    Failure = failure;
    Errors = errors;
  }

  public bool Succeeded { get; }

  public ApplicationOperationFailure Failure { get; }

  public IDictionary<string, string[]>? Errors { get; }

  public static ApplicationOperationResult Success() => new(true);

  public static ApplicationOperationResult Failed(
    ApplicationOperationFailure failure,
    IDictionary<string, string[]>? errors = null) =>
    new(false, failure, errors);

  public static ApplicationOperationResult Invalid(IEnumerable<ValidationFailure> failures) =>
    Failed(ApplicationOperationFailure.Validation, ValidationResult.Invalid(failures).ToErrorDictionary());
}

#pragma warning disable CA1000
public sealed class ApplicationOperationResult<TValue> : ApplicationOperationResult
{
  private ApplicationOperationResult(
    bool succeeded,
    TValue? value,
    ApplicationOperationFailure failure = ApplicationOperationFailure.None,
    IDictionary<string, string[]>? errors = null)
    : base(succeeded, failure, errors)
  {
    Value = value;
  }

  public TValue? Value { get; }

  public static ApplicationOperationResult<TValue> Success(TValue value) =>
    new(true, value);

  public new static ApplicationOperationResult<TValue> Failed(
    ApplicationOperationFailure failure,
    IDictionary<string, string[]>? errors = null) =>
    new(false, default, failure, errors);

  public new static ApplicationOperationResult<TValue> Invalid(IEnumerable<ValidationFailure> failures) =>
    Failed(ApplicationOperationFailure.Validation, ValidationResult.Invalid(failures).ToErrorDictionary());
}
#pragma warning restore CA1000
