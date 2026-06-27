using Alsappan.Api.Configuration;
using Alsappan.Api.Contracts;
using Alsappan.Api.Errors;
using Alsappan.Api.Modules;
using Alsappan.Api.OpenApi;
using Alsappan.Application.Common.Configuration;
using Alsappan.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiPlatform(builder.Configuration);
builder.Services.AddInfrastructure();
builder.Services.AddOpenApi(ApiConventions.CurrentVersion, options =>
{
  options.AddDocumentTransformer<AlsappanOpenApiDocumentTransformer>();
});
builder.Services.AddSingleton<ProblemDetailsMessageCatalog>();
builder.Services.AddSingleton<ApiProblemDetailsFactory>();
builder.Services.AddProblemDetails(options =>
{
  options.CustomizeProblemDetails = context =>
  {
    var factory = context.HttpContext.RequestServices.GetRequiredService<ApiProblemDetailsFactory>();
    var code = context.ProblemDetails.Status switch
    {
      StatusCodes.Status401Unauthorized => ApiProblemCode.Unauthorized,
      StatusCodes.Status403Forbidden => ApiProblemCode.Forbidden,
      StatusCodes.Status404NotFound => ApiProblemCode.NotFound,
      StatusCodes.Status409Conflict => ApiProblemCode.Conflict,
      StatusCodes.Status400BadRequest => ApiProblemCode.Validation,
      _ => ApiProblemCode.Unexpected
    };

    var localized = factory.Create(context.HttpContext, code, context.ProblemDetails.Status ?? StatusCodes.Status500InternalServerError);
    context.ProblemDetails.Title ??= localized.Title;
    context.ProblemDetails.Detail ??= localized.Detail;
    context.ProblemDetails.Type ??= localized.Type;
    context.ProblemDetails.Instance ??= localized.Instance;
    context.ProblemDetails.Extensions[ApiConventions.ErrorCodeExtension] = localized.Extensions[ApiConventions.ErrorCodeExtension];
    context.ProblemDetails.Extensions[ApiConventions.TraceIdExtension] = localized.Extensions[ApiConventions.TraceIdExtension];
  };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi("/openapi/{documentName}.json");
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseCors(ApiPlatformServiceCollectionExtensions.CorsPolicyName);
app.UseAuthentication();
app.UseMiddleware<OperationalLoggingMiddleware>();
app.UseAuthorization();

app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
      Predicate = registration => registration.Tags.Contains("live", StringComparer.Ordinal),
      ResponseWriter = OperationalHealthResponseWriter.WriteAsync
    })
    .WithName("System_Health")
    .WithTags("System");

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
      Predicate = registration => registration.Tags.Contains("ready", StringComparer.Ordinal),
      ResponseWriter = OperationalHealthResponseWriter.WriteAsync
    })
    .WithName("System_Readiness")
    .WithTags("System");

var v1 = app.MapGroup(ApiConventions.VersionPrefix);

v1.MapGet("/system/info", (IHostEnvironment environment, IOptions<AlsappanOptions> options) =>
    Results.Ok(new SystemInfoResponse(
        "Alsappan",
        environment.EnvironmentName,
        options.Value.Localization.DefaultCulture,
        options.Value.Localization.SupportedCultures.ToArray())))
    .WithName("System_GetInfo")
    .WithTags("System")
    .WithSummary("Returns API system metadata.")
    .WithDescription("Returns the application name, current environment, and supported cultures.")
    .Produces<SystemInfoResponse>()
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

v1.MapGet("/system/protected", () => Results.Ok(new ProtectedSystemResponse("authenticated")))
    .RequireAuthorization("AuthenticatedUser")
    .WithName("System_GetProtected")
    .WithTags("System")
    .WithSummary("Returns protected API smoke-test metadata.")
    .Produces<ProtectedSystemResponse>()
    .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
    .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

v1.MapEndpointModules();

app.Run();

#pragma warning disable CA1515
public partial class Program;
#pragma warning restore CA1515

internal sealed record SystemInfoResponse(
    string Application,
    string Environment,
    string DefaultCulture,
    string[] SupportedCultures);

internal sealed record ProtectedSystemResponse(string Status);
