using Alsappan.Application.Common.Validation;

namespace Alsappan.Application.Identity;

#pragma warning disable CA1000

public enum IdentityServiceResultStatus
{
  Success,
  ValidationFailed,
  Unauthorized,
  Forbidden,
  NotFound,
  Conflict
}

public sealed record IdentityServiceResult
{
  private IdentityServiceResult(
    IdentityServiceResultStatus status,
    ValidationResult validation,
    string? errorCode,
    string? message)
  {
    Status = status;
    Validation = validation;
    ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
    Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
  }

  public IdentityServiceResultStatus Status { get; }

  public ValidationResult Validation { get; }

  public string? ErrorCode { get; }

  public string? Message { get; }

  public bool Succeeded => Status == IdentityServiceResultStatus.Success;

  public static IdentityServiceResult Success() =>
    new(IdentityServiceResultStatus.Success, ValidationResult.Valid, null, null);

  public static IdentityServiceResult Invalid(IEnumerable<ValidationFailure> failures) =>
    new(IdentityServiceResultStatus.ValidationFailed, ValidationResult.Invalid(failures), "validation.failed", null);

  public static IdentityServiceResult Unauthorized(string errorCode = "auth.invalidCredentials", string? message = null) =>
    new(IdentityServiceResultStatus.Unauthorized, ValidationResult.Valid, errorCode, message);

  public static IdentityServiceResult Forbidden(string errorCode = "auth.permissionDenied", string? message = null) =>
    new(IdentityServiceResultStatus.Forbidden, ValidationResult.Valid, errorCode, message);

  public static IdentityServiceResult NotFound(string errorCode = "identity.notFound", string? message = null) =>
    new(IdentityServiceResultStatus.NotFound, ValidationResult.Valid, errorCode, message);

  public static IdentityServiceResult Conflict(string errorCode = "identity.conflict", string? message = null) =>
    new(IdentityServiceResultStatus.Conflict, ValidationResult.Valid, errorCode, message);
}

public sealed record IdentityServiceResult<T>
{
  private IdentityServiceResult(IdentityServiceResult result, T? value)
  {
    Result = result ?? throw new ArgumentNullException(nameof(result));
    Value = value;
  }

  public IdentityServiceResult Result { get; }

  public T? Value { get; }

  public IdentityServiceResultStatus Status => Result.Status;

  public ValidationResult Validation => Result.Validation;

  public string? ErrorCode => Result.ErrorCode;

  public string? Message => Result.Message;

  public bool Succeeded => Result.Succeeded;

  public static IdentityServiceResult<T> Success(T value)
  {
    ArgumentNullException.ThrowIfNull(value);
    return new IdentityServiceResult<T>(IdentityServiceResult.Success(), value);
  }

  public static IdentityServiceResult<T> Invalid(IEnumerable<ValidationFailure> failures) =>
    new(IdentityServiceResult.Invalid(failures), default);

  public static IdentityServiceResult<T> Unauthorized(string errorCode = "auth.invalidCredentials", string? message = null) =>
    new(IdentityServiceResult.Unauthorized(errorCode, message), default);

  public static IdentityServiceResult<T> Forbidden(string errorCode = "auth.permissionDenied", string? message = null) =>
    new(IdentityServiceResult.Forbidden(errorCode, message), default);

  public static IdentityServiceResult<T> NotFound(string errorCode = "identity.notFound", string? message = null) =>
    new(IdentityServiceResult.NotFound(errorCode, message), default);

  public static IdentityServiceResult<T> Conflict(string errorCode = "identity.conflict", string? message = null) =>
    new(IdentityServiceResult.Conflict(errorCode, message), default);
}
