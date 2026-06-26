using System.Globalization;
using System.Text;
using System.Text.Json;
using Alsappan.Api.Errors;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Configuration;

internal static class ApiPlatformServiceCollectionExtensions
{
  public const string CorsPolicyName = "AlsappanWeb";

  public static IServiceCollection AddApiPlatform(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    services.AddOptions<AlsappanOptions>()
        .Bind(configuration.GetSection(AlsappanOptions.SectionName))
        .Validate(options => !string.IsNullOrWhiteSpace(options.Database.ConnectionString), "Database connection string is required.")
        .Validate(options => !string.IsNullOrWhiteSpace(options.Auth.SigningKey), "Auth signing key is required.")
        .Validate(options => options.Auth.SigningKey.Length >= 32, "Auth signing key must be at least 32 characters.")
        .Validate(options => options.Localization.SupportedCultures.Count > 0, "At least one supported culture is required.")
        .ValidateOnStart();

    services.AddHttpContextAccessor();
    services.AddScoped<IClaimsPrincipalAccessor, HttpContextClaimsPrincipalAccessor>();
    services.AddScoped<IActiveOrganizationSelectionProvider, HttpHeaderActiveOrganizationSelectionProvider>();

    services.AddOptions<RequestLocalizationOptions>()
        .Configure<IOptions<AlsappanOptions>>((options, alsappanOptions) =>
        {
          var localization = alsappanOptions.Value.Localization;
          var supportedCultures = localization.SupportedCultures
              .Select(culture => new CultureInfo(culture))
              .ToArray();

          options.DefaultRequestCulture = new RequestCulture(localization.DefaultCulture);
          options.SupportedCultures = supportedCultures;
          options.SupportedUICultures = supportedCultures;
          options.ApplyCurrentCultureToResponseHeaders = true;
        });

    services.AddCors(options =>
    {
      options.AddPolicy(CorsPolicyName, policy =>
      {
        policy.WithOrigins(configuration.GetSection($"{AlsappanOptions.SectionName}:Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
      });
    });

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
          var auth = configuration.GetSection($"{AlsappanOptions.SectionName}:Auth").Get<AuthOptions>() ?? new AuthOptions();
          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = auth.Issuer,
            ValidAudience = auth.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
          };
          options.Events = new JwtBearerEvents
          {
            OnChallenge = async context =>
            {
              context.HandleResponse();
              context.Response.StatusCode = StatusCodes.Status401Unauthorized;
              await WriteProblemDetailsAsync(context.HttpContext, ApiProblemCode.Unauthorized, StatusCodes.Status401Unauthorized)
                  .ConfigureAwait(false);
            },
            OnForbidden = async context =>
            {
              context.Response.StatusCode = StatusCodes.Status403Forbidden;
              await WriteProblemDetailsAsync(context.HttpContext, ApiProblemCode.Forbidden, StatusCodes.Status403Forbidden)
                  .ConfigureAwait(false);
            }
          };
        });

    services.AddAuthorization(options =>
    {
      options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
    });

    services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API process is running."));

    return services;
  }

  private static Task WriteProblemDetailsAsync(HttpContext httpContext, string code, int statusCode)
  {
    var factory = httpContext.RequestServices.GetRequiredService<ApiProblemDetailsFactory>();
    var problem = factory.Create(httpContext, code, statusCode);

    httpContext.Response.ContentType = "application/problem+json";

    return JsonSerializer.SerializeAsync(
      httpContext.Response.Body,
      problem,
      problem.GetType(),
      cancellationToken: httpContext.RequestAborted);
  }
}
