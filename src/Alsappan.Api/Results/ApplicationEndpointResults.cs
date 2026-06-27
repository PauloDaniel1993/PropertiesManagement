using Alsappan.Api.Errors;
using Alsappan.Application.Common.Results;

namespace Alsappan.Api.OperationResults;

internal static class ApplicationEndpointResults
{
  public static IResult FromOperationResult<TValue>(
    HttpContext httpContext,
    ApplicationOperationResult<TValue> result)
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(result);

    return result.Succeeded
      ? Results.Ok(result.Value)
      : FromOperationFailure(httpContext, result.Failure, result.Errors);
  }

  public static IResult FromOperationResult(
    HttpContext httpContext,
    ApplicationOperationResult result)
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(result);

    return result.Succeeded
      ? Results.NoContent()
      : FromOperationFailure(httpContext, result.Failure, result.Errors);
  }

  private static IResult FromOperationFailure(
    HttpContext httpContext,
    ApplicationOperationFailure failure,
    IDictionary<string, string[]>? errors)
  {
    return failure switch
    {
      ApplicationOperationFailure.Validation => ApiProblemResults.Validation(
        httpContext,
        errors ?? new Dictionary<string, string[]> { ["request"] = ["validation"] }),
      ApplicationOperationFailure.Unauthorized => ApiProblemResults.Unauthorized(httpContext),
      ApplicationOperationFailure.Forbidden => ApiProblemResults.Forbidden(httpContext),
      ApplicationOperationFailure.NotFound => ApiProblemResults.NotFound(httpContext),
      ApplicationOperationFailure.Conflict => ApiProblemResults.Conflict(httpContext),
      _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
  }
}
