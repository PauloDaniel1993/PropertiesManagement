using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Errors;

internal static class ApiProblemResults
{
  public static IResult Validation(HttpContext httpContext, IDictionary<string, string[]> errors)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    var problem = GetFactory(httpContext).CreateValidation(httpContext, errors);
    return Results.Problem(problem);
  }

  public static IResult Unauthorized(HttpContext httpContext) =>
    Create(httpContext, ApiProblemCode.Unauthorized, StatusCodes.Status401Unauthorized);

  public static IResult Forbidden(HttpContext httpContext) =>
    Create(httpContext, ApiProblemCode.Forbidden, StatusCodes.Status403Forbidden);

  public static IResult NotFound(HttpContext httpContext) =>
    Create(httpContext, ApiProblemCode.NotFound, StatusCodes.Status404NotFound);

  public static IResult Conflict(HttpContext httpContext) =>
    Create(httpContext, ApiProblemCode.Conflict, StatusCodes.Status409Conflict);

  private static IResult Create(HttpContext httpContext, string code, int statusCode)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    var problem = GetFactory(httpContext).Create(httpContext, code, statusCode);
    return Results.Problem(problem);
  }

  private static ApiProblemDetailsFactory GetFactory(HttpContext httpContext) =>
    httpContext.RequestServices.GetRequiredService<ApiProblemDetailsFactory>();
}
