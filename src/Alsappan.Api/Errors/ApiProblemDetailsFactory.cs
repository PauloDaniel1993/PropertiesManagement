using Alsappan.Api.Contracts;
using Alsappan.Application.Common.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Alsappan.Api.Errors;

internal sealed class ApiProblemDetailsFactory(
  ProblemDetailsMessageCatalog messages,
  LocalizedValidationMessages validationMessages)
{
  private const string ProblemTypeBaseUri = "https://docs.alsappan.local/errors/";

  public ProblemDetails Create(HttpContext httpContext, string code, int statusCode, string? detail = null)
  {
    ArgumentNullException.ThrowIfNull(httpContext);

    var message = messages.Resolve(code, httpContext.Request.Headers.AcceptLanguage);
    var problem = new ProblemDetails
    {
      Status = statusCode,
      Title = message.Title,
      Detail = detail ?? message.Detail,
      Type = ProblemTypeBaseUri + code,
      Instance = httpContext.Request.Path
    };

    AddStandardExtensions(problem, httpContext, code);

    return problem;
  }

  public HttpValidationProblemDetails CreateValidation(
    HttpContext httpContext,
    IDictionary<string, string[]> errors)
  {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(errors);

    var culture = ProblemDetailsMessageCatalog.ResolveCulture(httpContext.Request.Headers.AcceptLanguage);
    var message = messages.Resolve(ApiProblemCode.Validation, culture);
    var problem = new HttpValidationProblemDetails(LocalizeErrors(errors, culture))
    {
      Status = StatusCodes.Status400BadRequest,
      Title = message.Title,
      Detail = message.Detail,
      Type = ProblemTypeBaseUri + ApiProblemCode.Validation,
      Instance = httpContext.Request.Path
    };

    AddStandardExtensions(problem, httpContext, ApiProblemCode.Validation);

    return problem;
  }

  private static void AddStandardExtensions(ProblemDetails problem, HttpContext httpContext, string code)
  {
    problem.Extensions[ApiConventions.ErrorCodeExtension] = code;
    problem.Extensions[ApiConventions.TraceIdExtension] = httpContext.TraceIdentifier;
  }

  private Dictionary<string, string[]> LocalizeErrors(IDictionary<string, string[]> errors, string culture)
  {
    var localizedErrors = new Dictionary<string, string[]>(errors.Count, StringComparer.Ordinal);

    foreach (var (field, fieldErrors) in errors)
    {
      localizedErrors[field] = fieldErrors
        .Select(error => validationMessages.Resolve(error, culture))
        .ToArray();
    }

    return localizedErrors;
  }
}
