using Alsappan.Api.Errors;
using Alsappan.Application.Identity;

namespace Alsappan.Api.Identity;

internal static class IdentityEndpointResults
{
  public static IResult FromServiceResult<TValue>(
    HttpContext httpContext,
    IdentityServiceResult<TValue> result)
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(result);

    return result.Succeeded
      ? Results.Ok(result.Value)
      : FromServiceResult(httpContext, result.Result);
  }

  public static IResult FromServiceResult(
    HttpContext httpContext,
    IdentityServiceResult result)
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(result);

    if (result.Succeeded)
    {
      return Results.NoContent();
    }

    return result.Status switch
    {
      IdentityServiceResultStatus.ValidationFailed => ApiProblemResults.Validation(
        httpContext,
        result.Validation.ToErrorDictionary()),
      IdentityServiceResultStatus.Unauthorized => ApiProblemResults.Unauthorized(httpContext),
      IdentityServiceResultStatus.Forbidden => ApiProblemResults.Forbidden(httpContext),
      IdentityServiceResultStatus.NotFound => ApiProblemResults.NotFound(httpContext),
      IdentityServiceResultStatus.Conflict => ApiProblemResults.Conflict(httpContext),
      _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
  }

  public static IResult FromOperationResult<TValue>(
    HttpContext httpContext,
    IdentityOperationResult<TValue> result)
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(result);

    return result.Succeeded
      ? Results.Ok(result.Value)
      : FromOperationFailure(httpContext, result.Failure, result.Errors);
  }

  public static IResult FromOperationResult(
    HttpContext httpContext,
    IdentityOperationResult result)
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(result);

    return result.Succeeded
      ? Results.NoContent()
      : FromOperationFailure(httpContext, result.Failure, result.Errors);
  }

  private static IResult FromOperationFailure(
    HttpContext httpContext,
    IdentityOperationFailure? failure,
    IDictionary<string, string[]>? errors)
  {
    return failure switch
    {
      IdentityOperationFailure.Validation => ApiProblemResults.Validation(
        httpContext,
        errors ?? new Dictionary<string, string[]> { ["request"] = ["validation"] }),
      IdentityOperationFailure.Unauthorized => ApiProblemResults.Unauthorized(httpContext),
      IdentityOperationFailure.Forbidden => ApiProblemResults.Forbidden(httpContext),
      IdentityOperationFailure.NotFound => ApiProblemResults.NotFound(httpContext),
      IdentityOperationFailure.Conflict => ApiProblemResults.Conflict(httpContext),
      _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
  }
}
